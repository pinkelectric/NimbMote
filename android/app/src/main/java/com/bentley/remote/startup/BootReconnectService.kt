package com.bentley.remote.startup

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.IBinder
import androidx.core.app.NotificationCompat
import androidx.core.app.ServiceCompat
import com.bentley.remote.MainActivity
import com.bentley.remote.R
import com.bentley.remote.data.RemoteRepository
import com.bentley.remote.network.RemoteTransport
import com.bentley.remote.security.SecretStore

/**
 * Foreground reconnection owner used only after normal device boot or an APK
 * replacement. It is a connectedDevice service because its one job is to keep
 * the authenticated LAN channel to the paired Windows device available.
 *
 * It intentionally does not create a MediaSession: targetSdk 35 does not allow
 * BOOT_COMPLETED to bootstrap the mediaPlayback service. Tapping this status
 * notification opens the Activity, which safely hands off to that full service.
 */
class BootReconnectService : Service() {
    private lateinit var transport: RemoteTransport

    override fun onCreate() {
        super.onCreate()
        RemoteRepository.initialize(this)
        createNotificationChannel()
        publishForeground(getString(R.string.boot_reconnect_starting))

        transport = RemoteTransport(
            secretStore = SecretStore(this),
            onMedia = RemoteRepository::mediaState,
            onVolume = RemoteRepository::volumeState,
            onAuthenticated = {
                publishForeground(
                    getString(
                        R.string.boot_reconnect_connected,
                        SecretStore(this).pairedClientName ?: getString(R.string.boot_reconnect_computer),
                    ),
                )
            },
            onConfirmedOffline = {
                publishForeground(getString(R.string.boot_reconnect_waiting))
            },
        )
        RemoteRepository.bind(transport)
        val settings = RemoteRepository.state.value
        transport.start(settings.reverseEnabled, settings.reverseHost)
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int = START_STICKY

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onDestroy() {
        transport.stop()
        RemoteRepository.unbind(transport)
        super.onDestroy()
    }

    private fun createNotificationChannel() {
        val manager = getSystemService(NotificationManager::class.java)
        manager.createNotificationChannel(
            NotificationChannel(
                BOOT_CHANNEL_ID,
                getString(R.string.boot_reconnect_channel_name),
                NotificationManager.IMPORTANCE_LOW,
            ).apply {
                description = getString(R.string.boot_reconnect_channel_description)
                setShowBadge(false)
            },
        )
    }

    private fun publishForeground(message: String) {
        val openApp = PendingIntent.getActivity(
            this,
            0,
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT,
        )
        val notification = NotificationCompat.Builder(this, BOOT_CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_stat_remote)
            .setContentTitle(getString(R.string.app_name))
            .setContentText(message)
            .setContentIntent(openApp)
            .setOngoing(true)
            .setCategory(NotificationCompat.CATEGORY_SERVICE)
            .setOnlyAlertOnce(true)
            .build()
        ServiceCompat.startForeground(
            this,
            BOOT_NOTIFICATION_ID,
            notification,
            ServiceInfo.FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE,
        )
    }

    companion object {
        private const val BOOT_CHANNEL_ID = "bentley_remote_reconnect"
        private const val BOOT_NOTIFICATION_ID = 1002

        fun start(context: Context, action: String) {
            val intent = Intent(context, BootReconnectService::class.java).setAction(action)
            androidx.core.content.ContextCompat.startForegroundService(context, intent)
        }

        fun stop(context: Context) {
            context.stopService(Intent(context, BootReconnectService::class.java))
        }
    }
}
