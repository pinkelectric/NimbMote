package com.bentley.remote

import org.junit.Assert.assertEquals
import org.junit.Test
import java.io.File

class PowerUiStaticTest {
    @Test fun primaryAndSecondaryPowerActionsAreNotDuplicated() {
        val source = File("src/main/java/com/bentley/remote/MainActivity.kt").readText()
        val primary = Regex("PrimaryPowerActions = listOf\\((.*?)\\)\\nprivate val Secondary", RegexOption.DOT_MATCHES_ALL)
            .find(source)!!.groupValues[1]
        val secondary = Regex("SecondaryPowerActions = listOf\\((.*?)\\)\\n\\n@Composable", RegexOption.DOT_MATCHES_ALL)
            .find(source)!!.groupValues[1]
        assertEquals(setOf("shutdown", "restart"), Regex("PowerUiAction\\(\"(.*?)\"").findAll(primary).map { it.groupValues[1] }.toSet())
        assertEquals(setOf("sleep", "lock"), Regex("PowerUiAction\\(\"(.*?)\"").findAll(secondary).map { it.groupValues[1] }.toSet())
    }
}
