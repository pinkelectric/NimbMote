package com.bentley.remote.media

import com.bentley.remote.model.RemoteMediaState
import org.junit.Assert.assertEquals
import org.junit.Test

class TimelinePredictorTest {
    @Test fun playing_advances_from_monotonic_snapshot() {
        val media = RemoteMediaState(playbackStatus = "playing", positionMs = 1_000, durationMs = 10_000, timelineReceivedAtElapsedMs = 5_000)
        assertEquals(1_500, TimelinePredictor.positionMs(media, 5_500))
    }

    @Test fun pause_and_seek_do_not_advance() {
        val media = RemoteMediaState(playbackStatus = "paused", positionMs = 7_000, durationMs = 10_000, timelineReceivedAtElapsedMs = 5_000)
        assertEquals(7_000, TimelinePredictor.positionMs(media, 99_000))
    }

    @Test fun duration_is_clamped_and_clock_never_rewinds_base() {
        val media = RemoteMediaState(playbackStatus = "playing", positionMs = 9_800, durationMs = 10_000, timelineReceivedAtElapsedMs = 5_000)
        assertEquals(10_000, TimelinePredictor.positionMs(media, 7_000))
        assertEquals(9_800, TimelinePredictor.positionMs(media, 4_000))
    }
}
