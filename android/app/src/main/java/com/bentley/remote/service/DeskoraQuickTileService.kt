package com.bentley.remote.service

import android.service.quicksettings.Tile
import android.service.quicksettings.TileService
import com.bentley.remote.R

/** A one-tap remote media toggle that remains available after Android hides the media card. */
class DeskoraQuickTileService : TileService() {
    override fun onStartListening() {
        super.onStartListening()
        updateTile()
    }

    override fun onClick() {
        super.onClick()
        BentleyRemoteService.toggleWindowsMedia(this)
        updateTile()
    }

    private fun updateTile() {
        qsTile?.apply {
            label = getString(R.string.quick_tile_label)
            state = Tile.STATE_INACTIVE
            updateTile()
        }
    }
}
