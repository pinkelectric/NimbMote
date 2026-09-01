package com.bentley.remote.media

import androidx.media3.common.Player

/** Keeps the legitimate paused remote-media card available without creating a generic status card. */
internal object RemoteMediaNotificationPolicy {
    fun shouldKeepRemoteControlsVisible(playbackState: Int, timelineIsEmpty: Boolean): Boolean =
        !timelineIsEmpty && playbackState == Player.STATE_READY
}
