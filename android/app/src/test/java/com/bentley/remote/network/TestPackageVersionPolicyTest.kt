package com.bentley.remote.network

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class TestPackageVersionPolicyTest {
    @Test fun DeskoraPackageIsShownOnlyWhenItIsNewer() {
        assertTrue(TestPackageVersionPolicy.shouldShow("Deskora v0.5.0", "0.4.1"))
        assertFalse(TestPackageVersionPolicy.shouldShow("Deskora v0.5.0", "0.5.0"))
        assertFalse(TestPackageVersionPolicy.shouldShow("Deskora v0.4.1", "0.5.0"))
        assertFalse(TestPackageVersionPolicy.shouldShow("Bentley Remote v0.5.0", "0.5.0"))
        assertFalse(TestPackageVersionPolicy.shouldShow("Deskora v0.5.0 — reconnect recovery", "0.5.0"))
    }

    @Test fun OtherProjectsAreNotHiddenByBentleysVersion() {
        assertTrue(TestPackageVersionPolicy.shouldShow("TouchGrass test build", "0.5.0"))
    }
}
