package com.bentley.remote.security

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import java.security.KeyStore
import java.util.UUID
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

class SecretStore(context: Context) {
    private val preferences = context.applicationContext.getSharedPreferences("bentley_remote_secure", Context.MODE_PRIVATE)

    val androidId: String
        get() = preferences.getString(AndroidIdKey, null) ?: UUID.randomUUID().toString().also {
            preferences.edit().putString(AndroidIdKey, it).apply()
        }

    val pairedClientId: String? get() = preferences.getString(PairedClientIdKey, null)
    val pairedClientName: String? get() = preferences.getString(PairedClientNameKey, null)
    val isPaired: Boolean get() = pairedClientId != null && getSecret() != null

    fun savePairing(clientId: String, clientName: String, secret: ByteArray) {
        require(secret.size == 32) { "Pairing secret must be 32 bytes" }
        val cipher = Cipher.getInstance(Transformation)
        cipher.init(Cipher.ENCRYPT_MODE, getOrCreateKey())
        val encrypted = cipher.doFinal(secret)
        preferences.edit()
            .putString(PairedClientIdKey, clientId)
            .putString(PairedClientNameKey, clientName)
            .putString(EncryptedSecretKey, Base64.encodeToString(encrypted, Base64.NO_WRAP))
            .putString(SecretIvKey, Base64.encodeToString(cipher.iv, Base64.NO_WRAP))
            .commit()
    }

    fun getSecret(): ByteArray? {
        val encrypted = preferences.getString(EncryptedSecretKey, null) ?: return null
        val iv = preferences.getString(SecretIvKey, null) ?: return null
        return try {
            val cipher = Cipher.getInstance(Transformation)
            cipher.init(
                Cipher.DECRYPT_MODE,
                getOrCreateKey(),
                GCMParameterSpec(128, Base64.decode(iv, Base64.NO_WRAP)),
            )
            cipher.doFinal(Base64.decode(encrypted, Base64.NO_WRAP))
        } catch (_: Exception) {
            null
        }
    }

    fun clearPairing() {
        preferences.edit()
            .remove(PairedClientIdKey)
            .remove(PairedClientNameKey)
            .remove(EncryptedSecretKey)
            .remove(SecretIvKey)
            .commit()
    }

    private fun getOrCreateKey(): SecretKey {
        val keyStore = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
        (keyStore.getKey(KeyAlias, null) as? SecretKey)?.let { return it }
        return KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore").run {
            init(
                KeyGenParameterSpec.Builder(
                    KeyAlias,
                    KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT,
                )
                    .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                    .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                    .build(),
            )
            generateKey()
        }
    }

    private companion object {
        const val KeyAlias = "bentley_remote_pairing_key_v1"
        const val Transformation = "AES/GCM/NoPadding"
        const val AndroidIdKey = "android_id"
        const val PairedClientIdKey = "paired_client_id"
        const val PairedClientNameKey = "paired_client_name"
        const val EncryptedSecretKey = "encrypted_secret"
        const val SecretIvKey = "secret_iv"
    }
}
