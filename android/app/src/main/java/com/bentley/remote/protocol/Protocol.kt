package com.bentley.remote.protocol

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import java.util.UUID

@Serializable
data class Envelope(
    val version: Int = 1,
    val type: String,
    val id: String = UUID.randomUUID().toString(),
    val replyTo: String? = null,
    val sentAt: Long = System.currentTimeMillis(),
    val payload: JsonObject,
)

object ProtocolCodec {
    val json = Json {
        ignoreUnknownKeys = true
        explicitNulls = false
        encodeDefaults = true
    }

    fun encode(type: String, payload: JsonObject, replyTo: String? = null): String =
        json.encodeToString(Envelope.serializer(), Envelope(type = type, replyTo = replyTo, payload = payload))

    fun decode(text: String): Envelope = json.decodeFromString(Envelope.serializer(), text).also {
        require(it.version == 1) { "Unsupported protocol version ${it.version}" }
    }
}

