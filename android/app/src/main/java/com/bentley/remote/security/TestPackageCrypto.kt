package com.bentley.remote.security

import android.util.Base64
import java.nio.charset.StandardCharsets
import javax.crypto.Cipher
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.SecretKeySpec

/** Domain-separated decryption for one chunk of a user-approved test APK. */
object TestPackageCrypto {
    private val salt = ByteArray(32)
    private val info = "bentley-remote/v1/test-package/aes-256-gcm".toByteArray(StandardCharsets.UTF_8)

    fun decrypt(secret: ByteArray, transferId: String, index: Int, total: Int, sha256: String,
        nonceBase64: String, ciphertextBase64: String): ByteArray {
        val nonce = Base64.decode(nonceBase64, Base64.NO_WRAP)
        val ciphertext = Base64.decode(ciphertextBase64, Base64.NO_WRAP)
        require(nonce.size == 12) { "invalid package nonce" }
        require(ciphertext.size in 17..(384 * 1024 + 16)) { "invalid package chunk" }
        val key = hkdf(secret, salt, info, 32)
        return try {
            Cipher.getInstance("AES/GCM/NoPadding").run {
                init(Cipher.DECRYPT_MODE, SecretKeySpec(key, "AES"), GCMParameterSpec(128, nonce))
                updateAAD(aad(transferId, index, total, sha256).toByteArray(StandardCharsets.UTF_8))
                doFinal(ciphertext)
            }
        } finally { key.fill(0) }
    }

    fun aad(transferId: String, index: Int, total: Int, sha256: String) =
        "bentley-remote/v1/test-package\n$transferId\n$index\n$total\n$sha256"

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
