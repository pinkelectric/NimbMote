# Build status — v0.2.2 — 2026-08-13

## Versioned build completed

- Canonical `VERSION`: `0.2.2`.
- Android: `compileDebugKotlin`, unit tests and `assembleDebug` passed; APK is
  versionName `0.2.2`, versionCode `2003`, v2-signed with the same certificate
  as v0.1.1/v0.2.1.
- Windows: Release build and self-contained win-x64 publish passed; ZIP expands
  to 473 files.
- Windows safe tests passed: power fake-controller, discovery/pairing security,
  loopback discovery, artwork revision and startup-readiness retry policy.
- Protocol validation and Git whitespace checks passed. No real system action ran.

## Stable test release

`releases/v0.2.2/` contains exactly the versioned debug APK, Windows x64 ZIP
and CHANGELOG with their SHA-256 hashes. The release commit is annotated tag
`v0.2.2`. Hardware validation of Windows reboot/autostart on third-party Wi-Fi
or hotspot remains in `TESTING.md`.
