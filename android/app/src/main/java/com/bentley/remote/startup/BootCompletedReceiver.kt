package com.bentley.remote.startup

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.bentley.remote.security.SecretStore

/**
 * Restores a previously paired remote after normal device boot or an APK update.
 *
 * Android 15+ does not permit a BOOT_COMPLETED receiver to start a
 * mediaPlayback foreground service. This receiver instead starts a dedicated
 * connectedDevice service for the authenticated LAN connection. That service
 * owns its own status notification; the MediaSessionService is resumed only
 * after the user opens Bentley Remote.
 */
class BootCompletedReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        val action = intent.action
        if (action != Intent.ACTION_BOOT_COMPLETED && action != Intent.ACTION_MY_PACKAGE_REPLACED) return

        if (!SecretStore(context).isPaired) {
            Log.i(TAG, "Ignoring $action: no saved pairing")
            return
        }

        try {
            BootReconnectService.start(context, action)
        } catch (error: RuntimeException) {
            // A restricted manufacturer configuration can still reject a boot
            // foreground-service start. Never crash the receiver or claim a
            // live connection; opening Bentley Remote manually remains safe.
            Log.w(TAG, "Could not restore paired session after $action", error)
        }
    }

    private companion object {
        const val TAG = "BentleyBootReceiver"
    }
}
