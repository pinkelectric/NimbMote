# NimbMote v0.10.0 — persistent reconnect test build

## Changed

- Android keeps a low-priority foreground connection notification while it is
  waiting for the Windows agent. This is intended to keep the local reconnect
  service alive through idle periods and agent restarts.
- The Quick Settings tile now says **Reconnect** when offline. Its first tap
  reconnects only; it never queues a delayed Play/Pause command. Once paired
  again, the tile controls media as usual.
- Holding the Quick Settings tile opens NimbMote instead of Android's app-info
  screen.
- Added a local-only connection diagnostics journal: Android exposes it from
  the three-dot menu, and the Windows tray has **Copy connection diagnostics**.
  It contains connection events only; it does not automatically upload anything
  and excludes pairing secrets, media data, audio, screenshots, and protocol
  payloads.

## Verification

- Android unit tests and protocol fixture validation passed.
- Android debug APK built as versionName `0.10.0` / versionCode `10001` and its
  APK Signature Scheme v2 signature was verified.
- Windows x64 self-contained agent built successfully.
- Inno Setup 6.7.3 compiled the Windows installer successfully.

## SHA-256

- `NimbMote-v0.10.0-debug.apk`
  `E701A83BAA54E07E705606099AC5376B52A0F0859CACE7865EB34AA17BC0B31B`
- `NimbMote-Setup-v0.10.0.exe`
  `5642633C81011D372A4F07A0B3EC9FACAD56D5E3A74951E3C9290F43D7F465ED`
