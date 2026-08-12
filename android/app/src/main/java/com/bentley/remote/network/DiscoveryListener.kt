package com.bentley.remote.network

import android.util.Base64
import android.util.Log
import com.bentley.remote.data.RemoteRepository
import com.bentley.remote.security.PairingCrypto
import com.bentley.remote.security.SecretStore
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.intOrNull
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.longOrNull
import kotlinx.serialization.json.put
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress
import java.net.InetSocketAddress
import java.net.Inet4Address
import java.net.NetworkInterface
import java.nio.charset.StandardCharsets
import java.security.MessageDigest
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.TimeUnit
import kotlin.math.abs

class DiscoveryListener(
    private val secretStore: SecretStore,
    private val scope: CoroutineScope,
) {
    private val seenNonces = ConcurrentHashMap<String, Long>()
    @Volatile private var socket: DatagramSocket? = null
    private var job: Job? = null

    data class BootstrapCandidate(val address: InetAddress, val agentId: String)

    fun start() {
        job = scope.launch(Dispatchers.IO) {
            val listener = DatagramSocket(null).apply {
                reuseAddress = true
                bind(InetSocketAddress(DiscoveryPort))
            }
            socket = listener
            val buffer = ByteArray(4096)
            while (isActive) {
                val packet = DatagramPacket(buffer, buffer.size)
                try { listener.receive(packet) } catch (_: Exception) { if (!isActive) break else continue }
                val data = packet.data.copyOfRange(packet.offset, packet.offset + packet.length)
                try {
                    val root = Json.parseToJsonElement(data.toString(StandardCharsets.UTF_8)) as? JsonObject ?: continue
                    when (root.string("type")) {
                        ProbeType -> handlePaired(root, packet.address, packet.port, listener)
                    }
                } catch (_: Exception) { }
            }
        }
    }

    fun bootstrapProbe(code: String): ByteArray {
        val timestamp = System.currentTimeMillis()
        val nonce = Base64.encodeToString(ByteArray(18).also(java.security.SecureRandom()::nextBytes), Base64.NO_WRAP)
        val body = listOf(BootstrapDomain, 1, "probe", secretStore.androidId, timestamp, nonce).joinToString("\n")
        val proof = Base64.encodeToString(
            PairingCrypto.hmac(PairingCrypto.codeKey(code), body.toByteArray(StandardCharsets.UTF_8)), Base64.NO_WRAP)
        return buildJsonObject {
            put("version", 1); put("type", BootstrapProbeType); put("phoneId", secretStore.androidId)
            put("timestamp", timestamp); put("nonce", nonce); put("proof", proof)
        }.toString().toByteArray(StandardCharsets.UTF_8)
    }

    fun findBootstrapCandidates(code: String, timeoutMs: Long = 3_000): List<BootstrapCandidate> {
        val probeBytes = bootstrapProbe(code)
        val probeRoot = Json.parseToJsonElement(probeBytes.toString(StandardCharsets.UTF_8)) as JsonObject
        val phoneId = probeRoot.string("phoneId")
        val timestamp = probeRoot["timestamp"]!!.jsonPrimitive.longOrNull!!
        val nonce = probeRoot.string("nonce")
        val candidates = LinkedHashMap<String, BootstrapCandidate>()
        DatagramSocket().use { udp ->
            udp.broadcast = true
            udp.soTimeout = 250
            broadcastAddresses().forEach { target ->
                try { udp.send(DatagramPacket(probeBytes, probeBytes.size, target, DiscoveryPort)) } catch (_: Exception) { }
            }
            val deadline = System.nanoTime() + TimeUnit.MILLISECONDS.toNanos(timeoutMs)
            val buffer = ByteArray(4096)
            while (System.nanoTime() < deadline) {
                val packet = DatagramPacket(buffer, buffer.size)
                try { udp.receive(packet) } catch (_: java.net.SocketTimeoutException) { continue }
                val root = try {
                    Json.parseToJsonElement(packet.data.copyOfRange(packet.offset, packet.offset + packet.length)
                        .toString(StandardCharsets.UTF_8)) as JsonObject
                } catch (_: Exception) { continue }
                if (root.string("type") != BootstrapResponseType || root.string("phoneId") != phoneId ||
                    root["timestamp"]?.jsonPrimitive?.longOrNull != timestamp || root.string("nonce") != nonce ||
                    root.string("requesterAddress") != udp.localAddress.hostAddress &&
                    root.string("requesterAddress") !in localAddresses() ||
                    root["requesterPort"]?.jsonPrimitive?.intOrNull != udp.localPort) continue
                val agentId = root.string("agentId")
                val requesterAddress = root.string("requesterAddress")
                val body = listOf(BootstrapDomain, 1, "response", agentId, phoneId, timestamp, nonce,
                    requesterAddress, udp.localPort).joinToString("\n")
                val supplied = decode(root.string("proof")) ?: continue
                if (!MessageDigest.isEqual(supplied,
                        PairingCrypto.hmac(PairingCrypto.codeKey(code), body.toByteArray(StandardCharsets.UTF_8)))) continue
                candidates["${packet.address.hostAddress}:$agentId"] = BootstrapCandidate(packet.address, agentId)
            }
        }
        return candidates.values.toList()
    }

    fun stop() { job?.cancel(); socket?.close(); socket = null }

    private fun handlePaired(root: JsonObject, source: InetAddress, sourcePort: Int, listener: DatagramSocket) {
        val secret = secretStore.getSecret() ?: return
        val clientId = root.string("clientId")
        val timestamp = root["timestamp"]?.jsonPrimitive?.longOrNull ?: return
        val nonce = root.string("nonce")
        if (root["version"]?.jsonPrimitive?.intOrNull != 1 || clientId != secretStore.pairedClientId ||
            abs(System.currentTimeMillis() - timestamp) > 120_000 || nonce.length < 16 ||
            seenNonces.containsKey(nonce)) return
        val probeBody = listOf(DiscoveryDomain, 1, "probe", clientId, timestamp, nonce).joinToString("\n")
        val supplied = decode(root.string("proof")) ?: return
        if (!MessageDigest.isEqual(supplied, PairingCrypto.hmac(secret, probeBody.toByteArray(StandardCharsets.UTF_8)))) return
        if (seenNonces.putIfAbsent(nonce, timestamp) != null) return
        seenNonces.entries.removeIf { it.value < timestamp - 300_000 }
        val requesterAddress = source.hostAddress ?: return
        val responseBody = listOf(DiscoveryDomain, 1, "response", clientId, secretStore.androidId,
            timestamp, nonce, requesterAddress, sourcePort, AndroidPort).joinToString("\n")
        val response = buildJsonObject {
            put("version", 1); put("type", ResponseType); put("clientId", clientId)
            put("serverId", secretStore.androidId); put("timestamp", timestamp); put("nonce", nonce)
            put("requesterAddress", requesterAddress); put("requesterPort", sourcePort); put("serverPort", AndroidPort)
            put("proof", Base64.encodeToString(
                PairingCrypto.hmac(secret, responseBody.toByteArray(StandardCharsets.UTF_8)), Base64.NO_WRAP))
        }.toString().toByteArray(StandardCharsets.UTF_8)
        listener.send(DatagramPacket(response, response.size, source, sourcePort))
        Log.d(LogTag, "Authenticated paired discovery from $requesterAddress")
    }

    private fun decode(value: String): ByteArray? = try { Base64.decode(value, Base64.NO_WRAP) } catch (_: Exception) { null }
    private fun JsonObject.string(name: String): String = this[name]?.jsonPrimitive?.contentOrNull ?: ""

    private fun broadcastAddresses(): Set<InetAddress> {
        val result = linkedSetOf(InetAddress.getByName("255.255.255.255"))
        NetworkInterface.getNetworkInterfaces()?.toList()?.filter { it.isUp && !it.isLoopback }?.forEach { network ->
            network.interfaceAddresses.mapNotNullTo(result) { it.broadcast }
        }
        return result
    }

    private fun localAddresses(): Set<String> = NetworkInterface.getNetworkInterfaces()?.toList()
        ?.flatMap { it.inetAddresses.toList() }
        ?.filterIsInstance<Inet4Address>()
        ?.mapNotNull { it.hostAddress }
        ?.toSet().orEmpty()

    companion object {
        const val DiscoveryPort = 45894
        const val AndroidPort = 45892
        const val ProbeType = "bentley.discovery.probe"
        const val ResponseType = "bentley.discovery.response"
        const val BootstrapProbeType = "bentley.bootstrap.probe"
        const val BootstrapResponseType = "bentley.bootstrap.response"
        const val DiscoveryDomain = "bentley-remote-discovery"
        const val BootstrapDomain = "bentley-remote-bootstrap"
        const val LogTag = "BentleyRemote.Discovery"
    }
}
