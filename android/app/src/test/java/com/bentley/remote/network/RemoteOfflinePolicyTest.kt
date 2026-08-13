package com.bentley.remote.network

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class RemoteOfflinePolicyTest {
    @Test fun heartbeatTimeoutReleasesImmediatelyButDisconnectHasGracePeriod() {
        assertEquals(0L, RemoteOfflinePolicy.releaseDelayMs(heartbeatTimedOut = true))
        assertEquals(15_000L, RemoteOfflinePolicy.releaseDelayMs(heartbeatTimedOut = false))
    }

    @Test fun onlyConfirmedRequestedShutdownOrRestartReleasesImmediately() {
        assertTrue(RemoteOfflinePolicy.shouldReleaseImmediatelyForPowerAction("shutdown", "shutdown", ok = true))
        assertTrue(RemoteOfflinePolicy.shouldReleaseImmediatelyForPowerAction("restart", "restart", ok = true))
        assertFalse(RemoteOfflinePolicy.shouldReleaseImmediatelyForPowerAction("shutdown", "shutdown", ok = false))
        assertFalse(RemoteOfflinePolicy.shouldReleaseImmediatelyForPowerAction("shutdown", "restart", ok = true))
        assertFalse(RemoteOfflinePolicy.shouldReleaseImmediatelyForPowerAction("sleep", "sleep", ok = true))
        assertFalse(RemoteOfflinePolicy.shouldReleaseImmediatelyForPowerAction(null, "shutdown", ok = true))
    }
}
