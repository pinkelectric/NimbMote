# Build status — v0.2.1 — 2026-08-13

## Versioned build completed

- Canonical `VERSION`: `0.2.1`.
- Android: `compileDebugKotlin`, unit tests and `assembleDebug` passed; APK is
  versionName `0.2.1`, versionCode `2002`, and v2-signed with the same debug
  certificate as v0.1.1.
- Windows: Release build and self-contained win-x64 publish passed; ZIP expands
  to 473 files.
- Protocol validation passed. Windows safe tests passed, including fake power
  actions, discovery/pairing security, loopback discovery and artwork revision.
- No test issued a real lock, sleep, restart or shutdown command.

## Stable test release

`releases/v0.2.1/` contains exactly the versioned debug APK, Windows x64 ZIP
and CHANGELOG with their SHA-256 hashes. Hardware checks are intentionally left
to `TESTING.md`; the release commit is tagged `v0.2.1` only after all local
checks above are complete.
