package com.bentley.remote.service

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.os.Handler
import android.os.Looper
import android.util.Log
import androidx.annotation.OptIn
import androidx.media3.common.util.UnstableApi
import androidx.media3.session.DefaultMediaNotificationProvider
import androidx.media3.session.MediaSession
import androidx.media3.session.MediaSessionService
import com.bentley.remote.MainActivity
import com.bentley.remote.R
import com.bentley.remote.data.RemoteRepository
import com.bentley.remote.media.RemotePlayer
import com.bentley.remote.network.RemoteTransport
import com.bentley.remote.security.SecretStore
import com.bentley.remote.startup.BootReconnectService

@OptIn(UnstableApi::class)
class BentleyRemoteService : MediaSessionService() {
    private val diagnosticsHandler = Handler(Looper.getMainLooper())
    private lateinit var transport: RemoteTransport
    private var player: RemotePlayer? = null
    private var mediaSession: MediaSession? = null

    override fun onCreate() {
        super.onCreate()
        // A user opening the app may be handing off from the boot-only
        // connectedDevice service. Keep only one listener on the LAN port.
        BootReconnectService.stop(this)
        RemoteRepository.initialize(this)
        configureMediaNotification()
        activateRemoteControls("service-started")
        transport = RemoteTransport(
            secretStore = SecretStore(this),
            onMedia = {
                RemoteRepository.mediaState(it)
                diagnosticsHandler.post { player?.updateMedia(it) }
            },
            onVolume = {
                RemoteRepository.volumeState(it)
                diagnosticsHandler.post { player?.updateVolume(it) }
            },
            onAuthenticated = { diagnosticsHandler.post { activateRemoteControls("authenticated") } },
            onConfirmedOffline = { reason -> diagnosticsHandler.post { deactivateRemoteControls(reason) } },
        )

        RemoteRepository.bind(transport)
        val settings = RemoteRepository.state.value
        transport.start(settings.reverseEnabled, settings.reverseHost)
    }

    override fun onGetSession(controllerInfo: MediaSession.ControllerInfo): MediaSession? = mediaSession

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        super.onStartCommand(intent, flags, startId)
        return START_STICKY
    }

    override fun onUpdateNotification(
        session: MediaSession,
        startInForegroundRequired: Boolean,
    ) {
        Log.i(
            TAG,
            "Media notification update requested: " +
                "startInForegroundRequired=$startInForegroundRequired, " +
                "sessionAdded=${isSessionAdded(session)}",
        )
        super.onUpdateNotification(session, startInForegroundRequired)
        diagnosticsHandler.post {
            logMediaNotificationState(session, "notification-updated")
        }
    }

    override fun onTaskRemoved(rootIntent: Intent?) {
        // Keep the remote session and reconnect loop alive after the Activity is dismissed.
    }

    override fun onDestroy() {
        transport.stop()
        RemoteRepository.unbind(transport)
        mediaSession?.release()
        mediaSession = null
        player?.release()
        player = null
        super.onDestroy()
    }

    private fun activateRemoteControls(reason: String) {
        if (mediaSession != null) return
        val remotePlayer = RemotePlayer()
        player = remotePlayer
        val activityIntent = PendingIntent.getActivity(
            this,
            0,
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT,
        )
        val session = MediaSession.Builder(this, remotePlayer)
            .setSessionActivity(activityIntent)
            .build()
        mediaSession = session
        // Registering the session explicitly lets MediaSessionService publish the Media3 card.
        addSession(session)
        logMediaNotificationState(session, "session-added:$reason")
    }

    private fun deactivateRemoteControls(reason: String) {
        val session = mediaSession ?: return
        mediaSession = null
        removeSession(session)
        session.release()
        player?.release()
        player = null
        getSystemService(NotificationManager::class.java).cancel(MEDIA_NOTIFICATION_ID)
        stopForeground(STOP_FOREGROUND_REMOVE)
        Log.i(TAG, "Remote controls released after confirmed offline: $reason")
    }

    private fun configureMediaNotification() {
        val notificationManager = getSystemService(NotificationManager::class.java)
        val channel = NotificationChannel(
            MEDIA_NOTIFICATION_CHANNEL_ID,
            getString(R.string.media_notification_channel_name),
            NotificationManager.IMPORTANCE_LOW,
        ).apply {
            description = getString(R.string.media_notification_channel_description)
            setShowBadge(false)
        }
        notificationManager.createNotificationChannel(channel)

        val notificationProvider = DefaultMediaNotificationProvider.Builder(this)
            .setChannelId(MEDIA_NOTIFICATION_CHANNEL_ID)
            .setChannelName(R.string.media_notification_channel_name)
            .setNotificationId(MEDIA_NOTIFICATION_ID)
            .build()
        setMediaNotificationProvider(notificationProvider)

        val installedChannel = notificationManager.getNotificationChannel(MEDIA_NOTIFICATION_CHANNEL_ID)
        Log.i(
            TAG,
            "Media notification channel: exists=${installedChannel != null}, " +
                "id=${installedChannel?.id}, importance=${installedChannel?.importance}",
        )
    }

    private fun logMediaNotificationState(session: MediaSession, event: String) {
        val notificationManager = getSystemService(NotificationManager::class.java)
        val channel = notificationManager.getNotificationChannel(MEDIA_NOTIFICATION_CHANNEL_ID)
        val notificationPublished = notificationManager.activeNotifications.any {
            it.id == MEDIA_NOTIFICATION_ID
        }
        Log.i(
            TAG,
            "Media notification diagnostics ($event): " +
                "channelExists=${channel != null}, channelId=${channel?.id}, " +
                "importance=${channel?.importance}, notificationPublished=$notificationPublished, " +
                "foreground=${isPlaybackOngoing()}, sessionAdded=${isSessionAdded(session)}, " +
                "mediaSessionActive=${mediaSession === session}, " +
                "timelineEmpty=${session.player.currentTimeline.isEmpty}, " +
                "playbackState=${session.player.playbackState}, " +
                "playWhenReady=${session.player.playWhenReady}",
        )
    }

    companion object {
        private const val TAG = "BentleyRemoteService"
        private const val MEDIA_NOTIFICATION_CHANNEL_ID = "bentley_remote_media"
        private const val MEDIA_NOTIFICATION_ID = 1001

        fun start(context: Context) {
            // A visible Activity is the permitted point at which the normal
            // mediaPlayback MediaSessionService may resume after device boot.
            BootReconnectService.stop(context)
            context.startService(Intent(context, BentleyRemoteService::class.java))
        }
    }
}
