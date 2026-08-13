# Build status — v0.2.3 — 2026-08-13

## Versioned build completed

- Canonical `VERSION`: `0.2.3`.
- Android: compile, unit tests and `assembleDebug` passed. APK versionName is
  `0.2.3`, versionCode `2004`; v2 signer matches v0.1.1–v0.2.2.
- Windows: Release build and self-contained win-x64 publish passed. The ZIP
  expands to 474 files and includes `Install-Or-Update.ps1` next to the EXE.
- Windows safe tests passed, including managed install path and registry-command
  intent. Installer `-WhatIf` passed and showed only Bentley Remote process,
  files and Run-value actions; it did not alter the real install, processes or
  registry.
- Protocol validation and Git whitespace checks passed. No system power command
  or real installation was run.

## Stable test release

`releases/v0.2.3/` contains exactly the debug APK, Windows x64 ZIP and
CHANGELOG. The release commit is annotated tag `v0.2.3`. The manual install,
upgrade and reboot-autostart checks remain in `TESTING.md`.
