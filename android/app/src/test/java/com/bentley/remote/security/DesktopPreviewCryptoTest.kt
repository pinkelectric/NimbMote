package com.bentley.remote.security

import org.junit.Assert.assertEquals
import org.junit.Test

class DesktopPreviewCryptoTest {
    @Test fun additionalAuthenticatedDataUsesProtocolLineFeeds() {
        assertEquals(
            "bentley-remote/v1/desktop-preview\nrequest-123\n1724371200000\nimage/jpeg",
            DesktopPreviewCrypto.aad("request-123", 1_724_371_200_000, "image/jpeg"),
        )
    }
}
