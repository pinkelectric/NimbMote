package com.bentley.remote.service

import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import androidx.annotation.OptIn
import androidx.media3.common.util.UnstableApi
import androidx.media3.session.MediaSession
import androidx.media3.session.MediaSessionService
import com.bentley.remote.MainActivity
import com.bentley.remote.data.RemoteRepository
import com.bentley.remote.media.RemotePlayer
import com.bentley.remote.network.RemoteTransport
import com.bentley.remote.security.SecretStore

@OptIn(UnstableApi::class)
class BentleyRemoteService : MediaSessionService() {
    private lateinit var player: RemotePlayer
    private lateinit var transport: RemoteTransport
    private var mediaSession: MediaSession? = null

    override fun onCreate() {
        super.onCreate()
        RemoteRepository.initialize(this)
        player = RemotePlayer()
        transport = RemoteTransport(
            secretStore = SecretStore(this),
            onMedia = {
                RemoteRepository.mediaState(it)
                player.updateMedia(it)
            },
            onVolume = {
                RemoteRepository.volumeState(it)
                player.updateVolume(it)
            },
        )
        val activityIntent = PendingIntent.getActivity(
            this,
            0,
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT,
        )
        mediaSession = MediaSession.Builder(this, player)
            .setSessionActivity(activityIntent)
            .build()
        RemoteRepository.bind(transport)
        val settings = RemoteRepository.state.value
        transport.start(settings.reverseEnabled, settings.reverseHost)
    }

    override fun onGetSession(controllerInfo: MediaSession.ControllerInfo): MediaSession? = mediaSession

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        super.onStartCommand(intent, flags, startId)
        return START_STICKY
    }

    override fun onTaskRemoved(rootIntent: Intent?) {
        // Keep the remote session and reconnect loop alive after the Activity is dismissed.
    }

    override fun onDestroy() {
        transport.stop()
        mediaSession?.release()
        mediaSession = null
        player.release()
        super.onDestroy()
    }

    companion object {
        fun start(context: Context) {
            context.startService(Intent(context, BentleyRemoteService::class.java))
        }
    }
}

