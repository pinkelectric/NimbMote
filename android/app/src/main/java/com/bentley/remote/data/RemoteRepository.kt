package com.bentley.remote.data

import android.content.Context
import com.bentley.remote.model.AppUiState
import com.bentley.remote.model.RemoteMediaState
import com.bentley.remote.model.RemoteVolumeState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update

interface RemoteCommands {
    fun media(action: String, positionMs: Long? = null, offsetMs: Long? = null)
    fun volume(action: String, level: Float? = null, delta: Float? = null)
    fun resetPairing()
    fun updateReverse(enabled: Boolean, host: String)
    fun beginPairing(code: String)
    fun systemAction(action: String)
}

object RemoteRepository {
    private const val PreferencesName = "bentley_remote_settings"
    private const val ReverseEnabledKey = "reverse_enabled"
    private const val ReverseHostKey = "reverse_host"

    private val mutableState = MutableStateFlow(AppUiState())
    val state: StateFlow<AppUiState> = mutableState.asStateFlow()
    @Volatile private var commands: RemoteCommands? = null
    @Volatile private var initialized = false

    fun initialize(context: Context) {
        if (initialized) return
        synchronized(this) {
            if (initialized) return
            val preferences = context.applicationContext.getSharedPreferences(PreferencesName, Context.MODE_PRIVATE)
            mutableState.update {
                it.copy(
                    reverseEnabled = preferences.getBoolean(ReverseEnabledKey, false),
                    reverseHost = preferences.getString(ReverseHostKey, "") ?: "",
                )
            }
            initialized = true
        }
    }

    fun bind(boundCommands: RemoteCommands?) { commands = boundCommands }
    /**
     * Do not clear a newer service's command channel during the boot-service →
     * MediaSessionService handoff.
     */
    fun unbind(owner: RemoteCommands) {
        if (commands === owner) commands = null
    }
    fun media(action: String, positionMs: Long? = null, offsetMs: Long? = null) =
        commands?.media(action, positionMs, offsetMs)
    fun volume(action: String, level: Float? = null, delta: Float? = null) =
        commands?.volume(action, level, delta)
    fun resetPairing() = commands?.resetPairing()
    fun beginPairing(code: String) = commands?.beginPairing(code)
    fun systemAction(action: String) = commands?.systemAction(action)

    fun updateReverse(context: Context, enabled: Boolean, host: String) {
        context.getSharedPreferences(PreferencesName, Context.MODE_PRIVATE).edit()
            .putBoolean(ReverseEnabledKey, enabled)
            .putString(ReverseHostKey, host.trim())
            .apply()
        mutableState.update { it.copy(reverseEnabled = enabled, reverseHost = host.trim()) }
        commands?.updateReverse(enabled, host.trim())
    }

    internal fun connection(connected: Boolean, label: String, error: String? = null) {
        mutableState.update { it.copy(connected = connected, connectionLabel = label, lastError = error) }
    }

    internal fun pairing(code: String, expiresAt: Long, pairedComputer: String?) {
        mutableState.update { it.copy(pairingCode = code, pairingExpiresAt = expiresAt, pairedComputer = pairedComputer) }
    }

    internal fun mediaState(media: RemoteMediaState) {
        mutableState.update { state ->
            // WebSocket is ordered, but never let a queued older timeline undo a newer seek.
            if (media.timelineReceivedAtElapsedMs < state.media.timelineReceivedAtElapsedMs) state
            else state.copy(media = media)
        }
    }
    internal fun volumeState(volume: RemoteVolumeState) { mutableState.update { it.copy(volume = volume) } }
    internal fun commandResult(message: String) { mutableState.update { it.copy(commandResult = message) } }

    /**
     * A confirmed remote-PC outage is different from a short reconnect.  Clear cached state here
     * so a newly-created MediaSession can never re-publish stale Windows metadata or volume.
     */
    internal fun remoteUnavailable(label: String) {
        mutableState.update {
            it.copy(
                connected = false,
                connectionLabel = label,
                media = RemoteMediaState(),
                volume = RemoteVolumeState(),
                commandResult = null,
            )
        }
    }
}
