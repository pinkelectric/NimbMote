package com.bentley.remote.model

data class RemoteMediaState(
    val hasSession: Boolean = false,
    val sessionId: String? = null,
    val sourceAppId: String? = null,
    val title: String = "Nothing playing",
    val artist: String = "",
    val playbackStatus: String = "closed",
    val positionMs: Long? = null,
    val durationMs: Long? = null,
    /** Monotonic receipt time of the authoritative Windows timeline snapshot. */
    val timelineReceivedAtElapsedMs: Long = 0,
    val canSeek: Boolean = false,
    val canPrevious: Boolean = false,
    val canNext: Boolean = false,
    val artworkMime: String? = null,
    val artwork: ByteArray? = null,
) {
    val isPlaying: Boolean get() = playbackStatus == "playing"

    override fun equals(other: Any?): Boolean = other is RemoteMediaState &&
        hasSession == other.hasSession && sessionId == other.sessionId &&
        sourceAppId == other.sourceAppId && title == other.title && artist == other.artist &&
        playbackStatus == other.playbackStatus && positionMs == other.positionMs &&
        durationMs == other.durationMs && canSeek == other.canSeek &&
        canPrevious == other.canPrevious && canNext == other.canNext &&
        artworkMime == other.artworkMime &&
        (artwork?.contentEquals(other.artwork ?: byteArrayOf()) ?: (other.artwork == null))

    override fun hashCode(): Int = 31 * title.hashCode() + (artwork?.contentHashCode() ?: 0)
}

data class RemoteVolumeState(
    val level: Float = 0f,
    val muted: Boolean = false,
)

data class AppUiState(
    val connected: Boolean = false,
    val connectionLabel: String = "Starting…",
    val pairedComputer: String? = null,
    val pairingCode: String = "------",
    val pairingExpiresAt: Long = 0,
    val reverseEnabled: Boolean = false,
    val reverseHost: String = "",
    val media: RemoteMediaState = RemoteMediaState(),
    val volume: RemoteVolumeState = RemoteVolumeState(),
    val lastError: String? = null,
    val commandResult: String? = null,
)
