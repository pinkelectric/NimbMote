package com.bentley.remote.media

import androidx.media3.common.Player
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class RemoteMediaNotificationPolicyTest {
    @Test fun keepsAReadyPausedRemoteSessionVisible() {
        assertTrue(RemoteMediaNotificationPolicy.shouldKeepRemoteControlsVisible(Player.STATE_READY, timelineIsEmpty = false))
    }

    @Test fun doesNotCreateAStatusCardWithoutRemoteMedia() {
        assertFalse(RemoteMediaNotificationPolicy.shouldKeepRemoteControlsVisible(Player.STATE_IDLE, timelineIsEmpty = true))
        assertFalse(RemoteMediaNotificationPolicy.shouldKeepRemoteControlsVisible(Player.STATE_BUFFERING, timelineIsEmpty = true))
    }
}
