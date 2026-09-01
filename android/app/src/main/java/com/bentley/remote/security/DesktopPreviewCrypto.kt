package com.bentley.remote.security

import android.util.Base64
import java.nio.charset.StandardCharsets
import javax.crypto.Cipher
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.SecretKeySpec

/** Decrypts an image only after the ordinary paired WebSocket authentication has completed. */
object DesktopPreviewCrypto {
    private val salt = ByteArray(32)
    private val info = "bentley-remote/v1/desktop-preview/aes-256-gcm".toByteArray(StandardCharsets.UTF_8)

    fun decrypt(secret: ByteArray, requestId: String, capturedAt: Long, mimeType: String,
        nonceBase64: String, ciphertextBase64: String): ByteArray {
        val nonce = Base64.decode(nonceBase64, Base64.NO_WRAP)
        val ciphertext = Base64.decode(ciphertextBase64, Base64.NO_WRAP)
        require(nonce.size == 12) { "invalid desktop preview nonce" }
        require(ciphertext.size in 17..(1_048_576 + 16)) { "desktop preview exceeds size limit" }
        val key = hkdf(secret, salt, info, 32)
        return try {
            Cipher.getInstance("AES/GCM/NoPadding").run {
                init(Cipher.DECRYPT_MODE, SecretKeySpec(key, "AES"), GCMParameterSpec(128, nonce))
                updateAAD(aad(requestId, capturedAt, mimeType).toByteArray(StandardCharsets.UTF_8))
                doFinal(ciphertext)
            }
        } finally { key.fill(0) }
    }

    fun aad(requestId: String, capturedAt: Long, mimeType: String) =
        "bentley-remote/v1/desktop-preview\n$requestId\n$capturedAt\n$mimeType"

    private fun hkdf(input: ByteArray, salt: ByteArray, info: ByteArray, length: Int): ByteArray {
        val prk = PairingCrypto.hmac(salt, input)
        val output = ByteArray(length)
        var previous = byteArrayOf()
        var offset = 0
        var counter = 1
        while (offset < length) {
            previous = PairingCrypto.hmac(prk, previous + info + byteArrayOf(counter++.toByte()))
            val take = minOf(previous.size, length - offset)
            previous.copyInto(output, offset, 0, take)
            offset += take
        }
        prk.fill(0)
        return output
    }
}
