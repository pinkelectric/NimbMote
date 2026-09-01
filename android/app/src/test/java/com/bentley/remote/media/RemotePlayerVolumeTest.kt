package com.bentley.remote.media

import org.junit.Assert.assertEquals
import org.junit.Test

class RemotePlayerVolumeTest {
    @Test fun relativeVolumeStepIsOnePercent() {
        assertEquals(0.01f, RemotePlayer.RelativeVolumeStep, 0.00001f)
    }
}
