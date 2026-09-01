package com.bentley.remote.media

import android.os.Handler
import android.os.Looper
import androidx.annotation.OptIn
import androidx.media3.common.C
import androidx.media3.common.DeviceInfo
import androidx.media3.common.MediaItem
import androidx.media3.common.MediaMetadata
import androidx.media3.common.Player
import androidx.media3.common.SimpleBasePlayer
import androidx.media3.common.util.UnstableApi
import com.bentley.remote.data.RemoteRepository
import com.bentley.remote.model.RemoteMediaState
import com.bentley.remote.model.RemoteVolumeState
import com.google.common.util.concurrent.Futures
import com.google.common.util.concurrent.ListenableFuture

@OptIn(UnstableApi::class)
class RemotePlayer : SimpleBasePlayer(Looper.getMainLooper()) {
    private val mainHandler = Handler(Looper.getMainLooper())
    private var media = RemoteMediaState()
    private var remoteVolume = RemoteVolumeState()

    private val remoteDevice = DeviceInfo.Builder(DeviceInfo.PLAYBACK_TYPE_REMOTE)
        .setMinVolume(0)
        .setMaxVolume(100)
        .build()

    override fun getState(): State {
        val builder = State.Builder()
            .setAvailableCommands(buildCommands())
            .setDeviceInfo(remoteDevice)
            .setDeviceVolume((remoteVolume.level * 100).toInt().coerceIn(0, 100))
            .setIsDeviceMuted(remoteVolume.muted)
            .setSeekBackIncrementMs(10_000)
            .setSeekForwardIncrementMs(10_000)

        if (!media.hasSession) return builder.setPlaybackState(Player.STATE_IDLE).build()

        val metadataBuilder = MediaMetadata.Builder()
            .setTitle(media.title)
            .setArtist(media.artist)
            .setDurationMs(media.durationMs)
        media.artwork?.let {
            metadataBuilder.setArtworkData(it, MediaMetadata.PICTURE_TYPE_FRONT_COVER)
        }
        val metadata = metadataBuilder.build()
        val itemId = media.sessionId ?: "windows-media"
        val item = MediaItem.Builder()
            .setMediaId(itemId)
            .setMediaMetadata(metadata)
            .build()
        val itemData = MediaItemData.Builder("current-$itemId")
            .setMediaItem(item)
            .setMediaMetadata(metadata)
            .setDurationUs(media.durationMs?.times(1_000) ?: C.TIME_UNSET)
            .setIsSeekable(media.canSeek)
            .build()

        val playlist = mutableListOf<MediaItemData>()
        if (media.canPrevious) playlist += placeholderItem("previous")
        val currentIndex = playlist.size
        playlist += itemData
        if (media.canNext) playlist += placeholderItem("next")

        val positionMs = media.positionMs?.coerceAtLeast(0)?.let { position ->
            media.durationMs?.let { position.coerceAtMost(it) } ?: position
        } ?: 0
        return builder
            .setPlaylist(playlist)
            .setCurrentMediaItemIndex(currentIndex)
            .setContentPositionMs(positionMs)
            .setPlaybackState(Player.STATE_READY)
            .setPlayWhenReady(media.isPlaying, Player.PLAY_WHEN_READY_CHANGE_REASON_REMOTE)
            .build()
    }

    fun updateMedia(next: RemoteMediaState) = onApplicationThread {
        media = next
        invalidateState()
    }

    fun updateVolume(next: RemoteVolumeState) = onApplicationThread {
        remoteVolume = next
        invalidateState()
    }

    override fun handleSetPlayWhenReady(playWhenReady: Boolean): ListenableFuture<*> {
        RemoteRepository.media(if (playWhenReady) "play" else "pause")
        return Futures.immediateVoidFuture()
    }

    override fun handleSeek(mediaItemIndex: Int, positionMs: Long, seekCommand: Int): ListenableFuture<*> {
        when (seekCommand) {
            Player.COMMAND_SEEK_TO_PREVIOUS_MEDIA_ITEM -> RemoteRepository.media("previous")
            Player.COMMAND_SEEK_TO_NEXT_MEDIA_ITEM -> RemoteRepository.media("next")
            else -> RemoteRepository.media("seek", positionMs = positionMs.coerceAtLeast(0))
        }
        return Futures.immediateVoidFuture()
    }

    override fun handleSetDeviceVolume(deviceVolume: Int, flags: Int): ListenableFuture<*> {
        RemoteRepository.volume("set", level = deviceVolume.coerceIn(0, 100) / 100f)
        return Futures.immediateVoidFuture()
    }

    override fun handleIncreaseDeviceVolume(flags: Int): ListenableFuture<*> {
        RemoteRepository.volume("change", delta = RelativeVolumeStep)
        return Futures.immediateVoidFuture()
    }

    override fun handleDecreaseDeviceVolume(flags: Int): ListenableFuture<*> {
        RemoteRepository.volume("change", delta = -RelativeVolumeStep)
        return Futures.immediateVoidFuture()
    }

    override fun handleSetDeviceMuted(muted: Boolean, flags: Int): ListenableFuture<*> {
        RemoteRepository.volume(if (muted) "mute" else "unmute")
        return Futures.immediateVoidFuture()
    }

    override fun handleRelease(): ListenableFuture<*> = Futures.immediateVoidFuture()

    private fun buildCommands(): Player.Commands = Player.Commands.Builder()
        .add(Player.COMMAND_PLAY_PAUSE)
        .add(Player.COMMAND_GET_CURRENT_MEDIA_ITEM)
        .add(Player.COMMAND_GET_TIMELINE)
        .add(Player.COMMAND_GET_METADATA)
        .addIf(Player.COMMAND_SEEK_IN_CURRENT_MEDIA_ITEM, media.canSeek)
        .addIf(Player.COMMAND_SEEK_BACK, media.canSeek)
        .addIf(Player.COMMAND_SEEK_FORWARD, media.canSeek)
        .addIf(Player.COMMAND_SEEK_TO_PREVIOUS_MEDIA_ITEM, media.canPrevious)
        .addIf(Player.COMMAND_SEEK_TO_NEXT_MEDIA_ITEM, media.canNext)
        .add(Player.COMMAND_GET_DEVICE_VOLUME)
        .add(Player.COMMAND_SET_DEVICE_VOLUME_WITH_FLAGS)
        .add(Player.COMMAND_ADJUST_DEVICE_VOLUME_WITH_FLAGS)
        .add(Player.COMMAND_RELEASE)
        .build()

    private fun placeholderItem(direction: String): MediaItemData {
        val item = MediaItem.Builder()
            .setMediaId("remote-$direction")
            .setMediaMetadata(MediaMetadata.Builder().setTitle(direction.replaceFirstChar { it.uppercase() }).build())
            .build()
        return MediaItemData.Builder("remote-$direction")
            .setMediaItem(item)
            .setDurationUs(C.TIME_UNSET)
            .setIsSeekable(false)
            .build()
    }

    private fun onApplicationThread(action: () -> Unit) {
        if (Looper.myLooper() == applicationLooper) action() else mainHandler.post(action)
    }

    internal companion object {
        const val RelativeVolumeStep = 0.01f
    }
}
