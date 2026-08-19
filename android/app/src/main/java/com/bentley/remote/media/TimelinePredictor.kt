package com.bentley.remote.media

import com.bentley.remote.model.RemoteMediaState

/** Pure monotonic prediction for the main-screen timeline; it never polls Windows. */
object TimelinePredictor {
    fun positionMs(media: RemoteMediaState, nowElapsedMs: Long): Long {
        val base = media.positionMs?.coerceAtLeast(0) ?: 0L
        val advanced = if (media.isPlaying && media.timelineReceivedAtElapsedMs > 0) {
            base + (nowElapsedMs - media.timelineReceivedAtElapsedMs).coerceAtLeast(0)
        } else base
        return media.durationMs?.takeIf { it >= 0 }?.let { advanced.coerceAtMost(it) } ?: advanced
    }
}
