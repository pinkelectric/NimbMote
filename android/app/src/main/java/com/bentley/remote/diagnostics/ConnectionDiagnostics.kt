package com.bentley.remote.diagnostics

import android.content.Context
import android.util.Log
import org.json.JSONArray
import org.json.JSONObject
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * A small on-device ring buffer for diagnosing an unreliable local connection.
 * It intentionally records lifecycle and transport events only: no media metadata,
 * pairing secret, audio, screenshot, or network payload is ever written here.
 */
object ConnectionDiagnostics {
    private const val PreferencesName = "nimbmote_connection_diagnostics"
    private const val EventsKey = "events"
    private const val MaxEvents = 240
    private const val Tag = "NimbMoteDiagnostics"
    private val lock = Any()

    fun record(context: Context, source: String, message: String) {
        val safeSource = source.replace(Whitespace, " ").take(40)
        val safeMessage = message.replace(Whitespace, " ").take(280)
        Log.i(Tag, "$safeSource: $safeMessage")
        synchronized(lock) {
            val preferences = context.applicationContext.getSharedPreferences(PreferencesName, Context.MODE_PRIVATE)
            val events = runCatching { JSONArray(preferences.getString(EventsKey, "[]")) }.getOrElse { JSONArray() }
            events.put(JSONObject().apply {
                put("at", System.currentTimeMillis())
                put("source", safeSource)
                put("message", safeMessage)
            })
            while (events.length() > MaxEvents) events.remove(0)
            // Commit instead of apply: after a system reclaim, the last useful lifecycle event
            // must be on disk before the process disappears.
            preferences.edit().putString(EventsKey, events.toString()).commit()
        }
    }

    fun report(context: Context, maximumEvents: Int = 80): String = synchronized(lock) {
        val preferences = context.applicationContext.getSharedPreferences(PreferencesName, Context.MODE_PRIVATE)
        val events = runCatching { JSONArray(preferences.getString(EventsKey, "[]")) }.getOrElse { JSONArray() }
        val start = (events.length() - maximumEvents.coerceAtLeast(1)).coerceAtLeast(0)
        buildString {
            append("NimbMote connection diagnostics (local only)\n")
            append("Events shown: ").append(events.length() - start).append('\n')
            for (index in start until events.length()) {
                val entry = events.optJSONObject(index) ?: continue
                append(format(entry.optLong("at")))
                    .append(" [").append(entry.optString("source", "unknown")).append("] ")
                    .append(entry.optString("message", ""))
                    .append('\n')
            }
        }
    }

    private fun format(timestamp: Long): String =
        SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS", Locale.US).format(Date(timestamp))

    private val Whitespace = Regex("[\\r\\n\\t]+")
}
