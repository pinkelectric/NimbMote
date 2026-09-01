package com.bentley.remote.security

import android.util.Base64
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.longOrNull
import kotlinx.serialization.json.put
import java.nio.charset.StandardCharsets
import java.security.KeyFactory
import java.security.KeyPairGenerator
import java.security.MessageDigest
import java.security.SecureRandom
import java.security.spec.ECGenParameterSpec
import java.security.spec.X509EncodedKeySpec
import javax.crypto.Cipher
import javax.crypto.KeyAgreement
import javax.crypto.Mac
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.SecretKeySpec
import kotlin.math.abs

object PairingCrypto {
    private const val Domain = "bentley-remote-pairing-v2"

    data class AcceptResult(
        val clientId: String,
        val clientName: String,
        val secret: ByteArray,
        val payload: JsonObject,
    )

    fun accept(request: JsonObject, pairingCode: String, androidId: String): AcceptResult {
        val clientId = request.string("clientId")
        val clientName = request.string("clientName").ifBlank { "Windows PC" }
        val timestamp = request["timestamp"]?.jsonPrimitive?.longOrNull
            ?: throw IllegalArgumentException("timestamp required")
        val nonce = request.string("nonce")
        val clientPublicKey = request.string("publicKey")
        if (clientId.isBlank() || nonce.length < 16 || clientPublicKey.isBlank())
            throw IllegalArgumentException("pairing request fields invalid")
        if (abs(System.currentTimeMillis() - timestamp) > 120_000)
            throw IllegalArgumentException("pairing request expired")
        val requestBody = requestBody(clientId, timestamp, nonce, clientPublicKey)
        val suppliedProof = decode(request.string("proof"))
        val expectedProof = hmac(codeKey(pairingCode), requestBody.toByteArray(StandardCharsets.UTF_8))
        if (!MessageDigest.isEqual(suppliedProof, expectedProof))
            throw IllegalArgumentException("pairing code proof rejected")

        val keyPairGenerator = KeyPairGenerator.getInstance("EC")
        keyPairGenerator.initialize(ECGenParameterSpec("secp256r1"))
        val keyPair = keyPairGenerator.generateKeyPair()
        val serverPublicKey = Base64.encodeToString(keyPair.public.encoded, Base64.NO_WRAP)
        val remotePublicKey = KeyFactory.getInstance("EC")
            .generatePublic(X509EncodedKeySpec(decode(clientPublicKey)))
        val agreement = KeyAgreement.getInstance("ECDH")
        agreement.init(keyPair.private)
        agreement.doPhase(remotePublicKey, true)
        val rawSecret = agreement.generateSecret()
        val transcript = transcript(clientId, androidId, timestamp, nonce, clientPublicKey, serverPublicKey)
            .toByteArray(StandardCharsets.UTF_8)
        val encryptionKey = hkdf(rawSecret, codeKey(pairingCode), transcript, 32)
        val pairingSecret = ByteArray(32).also(SecureRandom()::nextBytes)
        val iv = ByteArray(12).also(SecureRandom()::nextBytes)
        val encrypted = Cipher.getInstance("AES/GCM/NoPadding").run {
            init(Cipher.ENCRYPT_MODE, SecretKeySpec(encryptionKey, "AES"), GCMParameterSpec(128, iv))
            updateAAD(transcript)
            doFinal(pairingSecret)
        }
        rawSecret.fill(0)
        encryptionKey.fill(0)
        val ivBase64 = Base64.encodeToString(iv, Base64.NO_WRAP)
        val ciphertextBase64 = Base64.encodeToString(encrypted, Base64.NO_WRAP)
        val responseBody = responseBody(clientId, androidId, timestamp, nonce, clientPublicKey,
            serverPublicKey, ivBase64, ciphertextBase64)
        return AcceptResult(clientId, clientName, pairingSecret, buildJsonObject {
            put("serverId", androidId)
            put("serverName", android.os.Build.MODEL)
            put("clientId", clientId)
            put("timestamp", timestamp)
            put("nonce", nonce)
            put("publicKey", serverPublicKey)
            put("iv", ivBase64)
            put("ciphertext", ciphertextBase64)
            put("proof", Base64.encodeToString(
                hmac(codeKey(pairingCode), responseBody.toByteArray(StandardCharsets.UTF_8)), Base64.NO_WRAP))
            put("crypto", "ECDH-P256-HKDF-SHA256-AES-256-GCM")
        })
    }

    fun codeKey(code: String): ByteArray = MessageDigest.getInstance("SHA-256")
        .digest("bentley-remote-bootstrap\ncode\n$code".toByteArray(StandardCharsets.UTF_8))

    fun hmac(key: ByteArray, body: ByteArray): ByteArray = Mac.getInstance("HmacSHA256").run {
        init(SecretKeySpec(key, "HmacSHA256")); doFinal(body)
    }

    private fun requestBody(clientId: String, timestamp: Long, nonce: String, publicKey: String) =
        listOf(Domain, "request", clientId, timestamp.toString(), nonce, publicKey).joinToString("\n")
    private fun responseBody(clientId: String, serverId: String, timestamp: Long, nonce: String,
        clientPublicKey: String, serverPublicKey: String, iv: String, ciphertext: String) =
        listOf(Domain, "response", clientId, serverId, timestamp.toString(), nonce, clientPublicKey,
            serverPublicKey, iv, ciphertext).joinToString("\n")
    private fun transcript(clientId: String, serverId: String, timestamp: Long, nonce: String,
        clientPublicKey: String, serverPublicKey: String) =
        listOf(Domain, "transcript", clientId, serverId, timestamp.toString(), nonce, clientPublicKey,
            serverPublicKey).joinToString("\n")

    private fun hkdf(input: ByteArray, salt: ByteArray, info: ByteArray, length: Int): ByteArray {
        val prk = hmac(salt, input)
        val output = ByteArray(length)
        var previous = byteArrayOf()
        var offset = 0
        var counter = 1
        while (offset < length) {
            previous = hmac(prk, previous + info + byteArrayOf(counter++.toByte()))
            val take = minOf(previous.size, length - offset)
            previous.copyInto(output, offset, 0, take)
            offset += take
        }
        prk.fill(0)
        return output
    }

    private fun decode(value: String): ByteArray = Base64.decode(value, Base64.NO_WRAP)
    private fun JsonObject.string(name: String): String = this[name]?.jsonPrimitive?.content ?: ""
}
