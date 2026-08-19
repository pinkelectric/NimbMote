# Build status — v0.2.5 — 2026-08-19

- Android: debug unit tests and `assembleDebug` completed with local JDK 17/SDK
  35. Timeline predictor tests pass; the debug signer remains unchanged.
- Windows: Release build and safe console tests pass, including generation-based
  stale-artwork rejection. No real GSMTC, power action or installation was run.
- Setup: native Inno Setup 6.7.3 package compiled from the self-contained x64
  publish. Static inspection confirms the directory page, Start-menu icon,
  checked-by-default desktop task, `{app}` Run target and narrow legacy cleanup.
- Hardware validation remains in `TESTING.md`.
