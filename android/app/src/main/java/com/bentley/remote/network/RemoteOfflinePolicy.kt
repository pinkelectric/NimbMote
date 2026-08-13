package com.bentley.remote.network

/** Pure, testable policy separating a recoverable handoff from a confirmed remote-PC outage. */
internal object RemoteOfflinePolicy {
    const val transientDisconnectGraceMs = 15_000L

    fun releaseDelayMs(heartbeatTimedOut: Boolean): Long =
        if (heartbeatTimedOut) 0L else transientDisconnectGraceMs

    fun shouldReleaseImmediatelyForPowerAction(pendingAction: String?, acceptedAction: String?, ok: Boolean): Boolean =
        ok && pendingAction != null && pendingAction == acceptedAction &&
            pendingAction in setOf("shutdown", "restart")
}
