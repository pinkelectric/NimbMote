package com.bentley.remote

import org.junit.Assert.assertEquals
import org.junit.Test
import java.io.File

class PowerUiStaticTest {
    @Test fun primaryAndSecondaryPowerActionsAreNotDuplicated() {
        val source = File("src/main/java/com/bentley/remote/MainActivity.kt").readText()
        val actions = Regex("PowerUiAction\\(\"(shutdown|restart|sleep|lock)\"")
            .findAll(source)
            .map { it.groupValues[1] }
            .toList()
        assertEquals(listOf("shutdown", "restart", "sleep", "lock"), actions)
    }

    @Test fun mainPowerActionsHaveMaterialIconsAndRareConnectionFallbackIsInOverflow() {
        val source = File("src/main/java/com/bentley/remote/MainActivity.kt").readText()
        assertEquals(true, source.contains("Icons.Default.PowerSettingsNew"))
        assertEquals(true, source.contains("Icons.Default.RestartAlt"))
        assertEquals(true, source.contains("R.string.connection_settings"))
        assertEquals(false, source.contains("Fallback: phone connects to Windows"))
    }
}
