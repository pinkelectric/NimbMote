package com.bentley.remote.network

import android.util.Base64
import com.bentley.remote.data.RemoteCommands
import com.bentley.remote.data.RemoteRepository
import com.bentley.remote.model.RemoteMediaState
import com.bentley.remote.model.RemoteVolumeState
import com.bentley.remote.protocol.Envelope
import com.bentley.remote.protocol.ProtocolCodec
import com.bentley.remote.security.SecretStore
import com.bentley.remote.security.PairingCrypto
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.floatOrNull
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.longOrNull
import kotlinx.serialization.json.put
import org.java_websocket.WebSocket
import org.java_websocket.client.WebSocketClient
import org.java_websocket.handshake.ClientHandshake
import org.java_websocket.handshake.ServerHandshake
import org.java_websocket.server.WebSocketServer
import java.net.InetSocketAddress
import java.net.URI
import java.security.MessageDigest
import java.security.SecureRandom
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.TimeUnit
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec
import kotlin.math.abs

class RemoteTransport(
    private val secretStore: SecretStore,
    private val onMedia: (RemoteMediaState) -> Unit,
    private val onVolume: (RemoteVolumeState) -> Unit,
    private val onAuthenticated: () -> Unit = {},
    private val onConfirmedOffline: (String) -> Unit = {},
) : RemoteCommands {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private val authenticated = ConcurrentHashMap<WebSocket, Boolean>()
    private val seenNonces = ConcurrentHashMap<String, Long>()
    private val outgoingChallenges = ConcurrentHashMap<WebSocket, Pair<Long, String>>()
    private val socketGate = Any()
    @Volatile private var activeSocket: WebSocket? = null
    @Volatile private var reverseClient: WebSocketClient? = null
    @Volatile private var lastSeenAt = 0L
    @Volatile private var reverseEnabled = false
    @Volatile private var reverseHost = ""
    private var reverseJob: Job? = null
    private var heartbeatJob: Job? = null
    private var offlineJob: Job? = null
    private var pendingPowerAction: String? = null
    private var pendingPowerActionExpiry: Job? = null
    private val discovery = DiscoveryListener(secretStore, scope)
    private var pairingCode = "------"
    private var pairingExpiresAt = 0L

    private val server = object : WebSocketServer(InetSocketAddress("0.0.0.0", AndroidPort)) {
        override fun onOpen(connection: WebSocket, handshake: ClientHandshake) {
            RemoteRepository.connection(false, "Windows found; authenticating…")
        }

        override fun onClose(connection: WebSocket, code: Int, reason: String, remote: Boolean) {
            markSocketDisconnected(connection, "Disconnected; Windows will reconnect", confirmed = false)
        }

        override fun onMessage(connection: WebSocket, message: String) = handleMessage(connection, message, false)

        override fun onError(connection: WebSocket?, exception: Exception) {
            RemoteRepository.connection(false, "Android WebSocket: ${exception.message}", exception.message)
            connection?.let { markSocketDisconnected(it, "Windows connection failed; retrying", confirmed = false) }
        }

        override fun onStart() {
            RemoteRepository.connection(false, "Listening for Windows on port $AndroidPort")
        }
    }

    fun start(reverseEnabled: Boolean, reverseHost: String) {
        this.reverseEnabled = reverseEnabled
        this.reverseHost = reverseHost
        refreshPairingCode()
        server.isReuseAddr = true
        server.start()
        discovery.start()
        reverseJob = scope.launch { reverseLoop() }
        heartbeatJob = scope.launch { heartbeatLoop() }
    }

    override fun updateReverse(enabled: Boolean, host: String) {
        reverseEnabled = enabled
        reverseHost = host
        if (!enabled) reverseClient?.close()
    }

    override fun beginPairing(code: String) {
        if (secretStore.isPaired || code.length != 6 || code.any { !it.isDigit() }) {
            RemoteRepository.connection(false, "Enter the 6-digit code shown by Windows")
            return
        }
        pairingCode = code
        pairingExpiresAt = System.currentTimeMillis() + PairingWindowMs
        RemoteRepository.pairing("SEARCHING", pairingExpiresAt, null)
        RemoteRepository.connection(false, "Searching for Windows on this LAN…")
        scope.launch {
            val candidates = discovery.findBootstrapCandidates(code)
            if (candidates.isEmpty() && !secretStore.isPaired)
                RemoteRepository.connection(false, "Windows not found; check the code and network, then retry")
            else if (!secretStore.isPaired)
                RemoteRepository.connection(false, "Windows found; waiting for secure pairing…")
        }
    }

    override fun systemAction(action: String) {
        if (action !in SystemActions) {
            RemoteRepository.commandResult("Action rejected")
            return
        }
        if (action in ImmediateOfflineActions) pendingPowerAction = action
        if (send("system.action", buildJsonObject { put("action", action) })) {
            pendingPowerActionExpiry?.cancel()
            pendingPowerActionExpiry = scope.launch {
                delay(PowerActionConfirmationWindowMs)
                if (pendingPowerAction == action) pendingPowerAction = null
            }
            RemoteRepository.commandResult("Request sent: $action")
        } else {
            if (pendingPowerAction == action) pendingPowerAction = null
            RemoteRepository.commandResult("No authenticated Windows connection")
        }
    }

    override fun resetPairing() {
        secretStore.clearPairing()
        authenticated.keys.forEach { it.close(1000, "pairing reset") }
        authenticated.clear()
        activeSocket = null
        offlineJob?.cancel()
        RemoteRepository.remoteUnavailable("Pairing reset; enter the new code on Windows")
        onConfirmedOffline("pairing reset")
        refreshPairingCode()
        RemoteRepository.connection(false, "Pairing reset; enter the new code on Windows")
    }

    override fun media(action: String, positionMs: Long?, offsetMs: Long?) {
        send("command.media", buildJsonObject {
            put("action", action)
            positionMs?.let { put("positionMs", it) }
            offsetMs?.let { put("offsetMs", it) }
        })
    }

    override fun volume(action: String, level: Float?, delta: Float?) {
        send("command.volume", buildJsonObject {
            put("action", action)
            level?.let { put("level", it.coerceIn(0f, 1f)) }
            delta?.let { put("delta", it) }
        })
    }

    private fun refreshPairingCode() {
        if (secretStore.isPaired) {
            pairingCode = "PAIRED"
            pairingExpiresAt = 0
        } else {
            pairingCode = "------"
            pairingExpiresAt = 0
        }
        RemoteRepository.pairing(if (secretStore.isPaired) "PAIRED" else "UNPAIRED",
            pairingExpiresAt, secretStore.pairedClientName)
    }

    private fun handleMessage(connection: WebSocket, text: String, isReverse: Boolean) {
        if (text.length > MaxMessageChars) {
            connection.close(1009, "message too large")
            return
        }
        val message = try {
            ProtocolCodec.decode(text)
        } catch (exception: Exception) {
            connection.close(1007, exception.message ?: "invalid JSON")
            return
        }
        if (authenticated[connection] == true) lastSeenAt = System.currentTimeMillis()
        when (message.type) {
            "pair.request.v2" -> handlePairRequestV2(connection, message)
            "auth.hello" -> handleAuthHello(connection, message)
            "auth.ok" -> if (isReverse) {
                val reason = validateAuthOk(connection, message.payload)
                if (reason == null) authenticate(connection, "Connected to Windows (fallback mode)")
                else {
                    RemoteRepository.connection(false, "Windows authentication failed: $reason")
                    connection.close(1008, reason)
                }
            }
            "auth.error" -> RemoteRepository.connection(false, "Authentication rejected by Windows")
            "heartbeat.ping" -> if (authenticated[connection] == true) sendTo(connection, "heartbeat.pong", buildJsonObject {
                put("nonce", message.payload.string("nonce"))
            }, message.id)
            "heartbeat.pong" -> if (authenticated[connection] == true) Unit else connection.close(1008, "authentication required")
            "command.result" -> if (authenticated[connection] == true) {
                val ok = message.payload.boolean("ok")
                val error = message.payload.string("error")
                RemoteRepository.commandResult(if (ok) "Windows accepted the action" else error.ifBlank { "Windows rejected the action" })
                val action = message.payload.stringOrNull("action")
                if (RemoteOfflinePolicy.shouldReleaseImmediatelyForPowerAction(pendingPowerAction, action, ok)) {
                    pendingPowerAction = null
                    pendingPowerActionExpiry?.cancel()
                    val label = "Windows accepted $action; remote controls released"
                    RemoteRepository.remoteUnavailable(label)
                    onConfirmedOffline(label)
                } else if (action != null && action == pendingPowerAction) {
                    pendingPowerAction = null
                    pendingPowerActionExpiry?.cancel()
                }
            }
            else -> {
                if (authenticated[connection] != true) {
                    connection.close(1008, "authentication required")
                    return
                }
                when (message.type) {
                    "state.snapshot" -> {
                        message.payload["media"]?.jsonObject?.let(::applyMedia)
                        message.payload["volume"]?.jsonObject?.let(::applyVolume)
                    }
                    "state.media" -> applyMedia(message.payload)
                    "state.volume" -> applyVolume(message.payload)
                }
            }
        }
    }

    private fun handlePairRequestV2(connection: WebSocket, message: Envelope) {
        if (secretStore.isPaired || pairingCode.length != 6 || System.currentTimeMillis() > pairingExpiresAt) {
            sendTo(connection, "pair.reject", buildJsonObject { put("reason", "pairing window closed") }, message.id)
            return
        }
        val accepted = try { PairingCrypto.accept(message.payload, pairingCode, secretStore.androidId) }
        catch (_: Exception) {
            sendTo(connection, "pair.reject", buildJsonObject { put("reason", "pairing verification failed") }, message.id)
            return
        }
        try {
            sendTo(connection, "pair.accept.v2", accepted.payload, message.id)
            secretStore.savePairing(accepted.clientId, accepted.clientName, accepted.secret)
        } finally {
            accepted.secret.fill(0)
        }
        authenticate(connection, "Connected securely to ${accepted.clientName}")
        refreshPairingCode()
    }

    private fun handleAuthHello(connection: WebSocket, message: Envelope) {
        val reason = validateAuth(message.payload)
        if (reason != null) {
            sendTo(connection, "auth.error", buildJsonObject { put("reason", reason) }, message.id)
            connection.close(1008, reason)
            return
        }
        sendTo(connection, "auth.ok", buildJsonObject {
            put("serverId", secretStore.androidId)
            put("serverName", android.os.Build.MODEL)
            val timestamp = message.payload["timestamp"]!!.jsonPrimitive.longOrNull!!
            val nonce = message.payload.string("nonce")
            put("timestamp", timestamp)
            put("nonce", nonce)
            put("proof", Base64.encodeToString(
                hmac(secretStore.getSecret()!!, "${secretStore.androidId}\n$timestamp\n$nonce".toByteArray()),
                Base64.NO_WRAP,
            ))
        }, message.id)
        authenticate(connection, "Connected to ${secretStore.pairedClientName ?: "Windows"}")
    }

    private fun authenticate(connection: WebSocket, label: String) {
        synchronized(socketGate) {
            val existing = activeSocket
            if (existing?.isOpen == true && existing !== connection && authenticated[existing] == true) {
                connection.close(1008, "another authenticated connection is active")
                return
            }
            authenticated[connection] = true
            activeSocket = connection
        }
        offlineJob?.cancel()
        lastSeenAt = System.currentTimeMillis()
        RemoteRepository.connection(true, label)
        onAuthenticated()
    }

    /**
     * A close notification can be a Wi-Fi handoff.  Keep the remote controls alive for a short,
     * testable grace period, unless the heartbeat has already proved that the PC is unavailable.
     */
    private fun markSocketDisconnected(connection: WebSocket, label: String, confirmed: Boolean) {
        authenticated.remove(connection)
        outgoingChallenges.remove(connection)
        val wasActive = synchronized(socketGate) {
            if (activeSocket === connection) {
                activeSocket = null
                true
            } else false
        }
        if (!wasActive) return
        RemoteRepository.connection(false, label)
        scheduleOfflineRelease(label, RemoteOfflinePolicy.releaseDelayMs(confirmed))
    }

    private fun scheduleOfflineRelease(label: String, delayMs: Long) {
        offlineJob?.cancel()
        pendingPowerActionExpiry?.cancel()
        offlineJob = scope.launch {
            if (delayMs > 0) delay(delayMs)
            if (activeSocket == null) {
                RemoteRepository.remoteUnavailable(label)
                onConfirmedOffline(label)
            }
        }
    }

    private fun validateAuth(payload: JsonObject): String? {
        val secret = secretStore.getSecret() ?: return "not paired"
        val clientId = payload.string("clientId")
        if (clientId != secretStore.pairedClientId) return "unknown device"
        val timestamp = payload["timestamp"]?.jsonPrimitive?.longOrNull ?: return "timestamp required"
        if (abs(System.currentTimeMillis() - timestamp) > 120_000) return "clock outside authentication window"
        val nonce = payload.string("nonce")
        if (nonce.length < 16 || seenNonces.containsKey(nonce)) return "replayed nonce"
        val supplied = try { Base64.decode(payload.string("proof"), Base64.NO_WRAP) } catch (_: IllegalArgumentException) { return "malformed proof" }
        val expected = hmac(secret, "$clientId\n$timestamp\n$nonce".toByteArray())
        if (!MessageDigest.isEqual(supplied, expected)) return "bad proof"
        if (seenNonces.putIfAbsent(nonce, timestamp) != null) return "replayed nonce"
        seenNonces.entries.removeIf { it.value < timestamp - 300_000 }
        return null
    }

    private fun validateAuthOk(connection: WebSocket, payload: JsonObject): String? {
        val secret = secretStore.getSecret() ?: return "not paired"
        val serverId = payload.string("serverId")
        if (serverId != secretStore.pairedClientId) return "unexpected Windows id"
        val challenge = outgoingChallenges.remove(connection) ?: return "missing challenge"
        val timestamp = payload["timestamp"]?.jsonPrimitive?.longOrNull ?: return "timestamp required"
        val nonce = payload.string("nonce")
        if (timestamp != challenge.first || nonce != challenge.second) return "challenge mismatch"
        val supplied = try { Base64.decode(payload.string("proof"), Base64.NO_WRAP) } catch (_: IllegalArgumentException) {
            return "malformed server proof"
        }
        val expected = hmac(secret, "$serverId\n$timestamp\n$nonce".toByteArray())
        return if (MessageDigest.isEqual(supplied, expected)) null else "bad server proof"
    }

    private fun applyMedia(payload: JsonObject) {
        val artwork = payload.stringOrNull("artworkBase64")?.let {
            try {
                Base64.decode(it, Base64.NO_WRAP).takeIf { bytes -> bytes.size <= MaxArtworkBytes }
            } catch (_: IllegalArgumentException) { null }
        }
        onMedia(
            RemoteMediaState(
                hasSession = payload.boolean("hasSession"),
                sessionId = payload.stringOrNull("sessionId"),
                sourceAppId = payload.stringOrNull("sourceAppId"),
                title = payload.string("title").ifBlank { "Windows media" },
                artist = payload.string("artist"),
                playbackStatus = payload.string("playbackStatus").ifBlank { "unknown" },
                positionMs = payload.longOrNull("positionMs"),
                durationMs = payload.longOrNull("durationMs"),
                canSeek = payload.boolean("canSeek"),
                canPrevious = payload.boolean("canPrevious"),
                canNext = payload.boolean("canNext"),
                artworkMime = payload.stringOrNull("artworkMime"),
                artwork = artwork,
            ),
        )
    }

    private fun applyVolume(payload: JsonObject) {
        onVolume(
            RemoteVolumeState(
                level = (payload["level"]?.jsonPrimitive?.floatOrNull ?: 0f).coerceIn(0f, 1f),
                muted = payload.boolean("muted"),
            ),
        )
    }

    private fun send(type: String, payload: JsonObject): Boolean {
        val socket = activeSocket
        if (socket?.isOpen == true && authenticated[socket] == true) {
            sendTo(socket, type, payload)
            return true
        }
        return false
    }

    private fun sendTo(socket: WebSocket, type: String, payload: JsonObject, replyTo: String? = null) {
        if (socket.isOpen) socket.send(ProtocolCodec.encode(type, payload, replyTo))
    }

    private suspend fun reverseLoop() {
        var backoffSeconds = 1L
        while (scope.isActive) {
            if (!reverseEnabled || reverseHost.isBlank() || !secretStore.isPaired || activeSocket?.isOpen == true) {
                delay(2_000)
                continue
            }
            val host = reverseHost.trim().removePrefix("ws://").substringBefore('/')
            val uri = try { URI("ws://$host:$ReversePort/bentley/") } catch (_: Exception) {
                RemoteRepository.connection(false, "Invalid Windows fallback address")
                delay(5_000)
                continue
            }
            val client = object : WebSocketClient(uri) {
                override fun onOpen(handshakeData: ServerHandshake) {
                    reverseClient = this
                    sendAuthHello(this)
                }

                override fun onMessage(message: String) = handleMessage(this, message, true)

                override fun onClose(code: Int, reason: String, remote: Boolean) {
                    markSocketDisconnected(this, "Fallback disconnected; Windows will reconnect", confirmed = false)
                    if (reverseClient === this) reverseClient = null
                }

                override fun onError(exception: Exception) {
                    RemoteRepository.connection(false, "Fallback unavailable: ${exception.message}")
                }
            }
            reverseClient = client
            try {
                RemoteRepository.connection(false, "Trying Windows fallback $host…")
                if (client.connectBlocking(5, TimeUnit.SECONDS)) {
                    backoffSeconds = 1
                    while (client.isOpen && scope.isActive && reverseEnabled) delay(1_000)
                }
            } catch (_: InterruptedException) {
                Thread.currentThread().interrupt()
            } catch (_: Exception) {
                // Retried below with bounded backoff.
            } finally {
                if (client.isOpen) client.close()
                if (reverseClient === client) reverseClient = null
            }
            delay(backoffSeconds * 1_000)
            backoffSeconds = (backoffSeconds * 2).coerceAtMost(10)
        }
    }

    private fun sendAuthHello(socket: WebSocket) {
        val secret = secretStore.getSecret() ?: return
        val timestamp = System.currentTimeMillis()
        val nonce = Base64.encodeToString(ByteArray(18).also(SecureRandom()::nextBytes), Base64.NO_WRAP)
        val clientId = secretStore.androidId
        val proof = Base64.encodeToString(hmac(secret, "$clientId\n$timestamp\n$nonce".toByteArray()), Base64.NO_WRAP)
        outgoingChallenges[socket] = timestamp to nonce
        sendTo(socket, "auth.hello", buildJsonObject {
            put("clientId", clientId)
            put("timestamp", timestamp)
            put("nonce", nonce)
            put("proof", proof)
        })
    }

    private suspend fun heartbeatLoop() {
        while (scope.isActive) {
            delay(10_000)
            val socket = activeSocket ?: continue
            if (System.currentTimeMillis() - lastSeenAt > 35_000) {
                // A hard PC shutdown may not produce TCP FIN/RST.  This is the positive
                // liveness signal: no authenticated message for the full heartbeat window.
                markSocketDisconnected(socket, "Windows unavailable (heartbeat timeout)", confirmed = true)
                socket.close(1001, "heartbeat timeout")
            } else {
                sendTo(socket, "heartbeat.ping", buildJsonObject {
                    put("nonce", Base64.encodeToString(ByteArray(12).also(SecureRandom()::nextBytes), Base64.NO_WRAP))
                })
            }
        }
    }

    fun stop() {
        discovery.stop()
        heartbeatJob?.cancel()
        reverseJob?.cancel()
        offlineJob?.cancel()
        reverseClient?.close()
        authenticated.keys.forEach { it.close(1001, "service stopping") }
        try { server.stop(1_000) } catch (_: Exception) { }
        scope.cancel()
        RemoteRepository.bind(null)
    }

    private fun hmac(secret: ByteArray, body: ByteArray): ByteArray = Mac.getInstance("HmacSHA256").run {
        init(SecretKeySpec(secret, "HmacSHA256"))
        doFinal(body)
    }

    private companion object {
        const val AndroidPort = 45892
        const val ReversePort = 45893
        const val PairingWindowMs = 10 * 60 * 1_000L
        const val MaxMessageChars = 1_500_000
        const val MaxArtworkBytes = 512 * 1024
        const val PowerActionConfirmationWindowMs = 10_000L
        val ImmediateOfflineActions = setOf("shutdown", "restart")
        val SystemActions = setOf("lock", "sleep", "restart", "shutdown")
    }
}

private fun JsonObject.string(name: String): String = this[name]?.jsonPrimitive?.contentOrNull ?: ""
private fun JsonObject.stringOrNull(name: String): String? = this[name]?.jsonPrimitive?.contentOrNull
private fun JsonObject.boolean(name: String): Boolean = this[name]?.jsonPrimitive?.booleanOrNull ?: false
private fun JsonObject.longOrNull(name: String): Long? = this[name]?.jsonPrimitive?.longOrNull
