package com.bentley.remote.service

import android.service.quicksettings.Tile
import android.service.quicksettings.TileService
import com.bentley.remote.R
import com.bentley.remote.data.RemoteRepository

/** A one-tap remote media toggle that remains available after Android hides the media card. */
class DeskoraQuickTileService : TileService() {
    override fun onStartListening() {
        super.onStartListening()
        RemoteRepository.initialize(this)
        updateTile()
    }

    override fun onClick() {
        super.onClick()
        BentleyRemoteService.toggleWindowsMedia(this)
        updateTile()
    }

    private fun updateTile() {
        val connected = RemoteRepository.state.value.connected
        qsTile?.apply {
            label = getString(if (connected) R.string.quick_tile_label else R.string.quick_tile_reconnect)
            state = if (connected) Tile.STATE_ACTIVE else Tile.STATE_INACTIVE
            updateTile()
        }
    }
}
