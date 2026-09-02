# NimbMote v0.9.0 — public rebrand

## Changed

- Renamed the Android app, Windows tray agent, pairing flow, Quick Settings tile,
  installer, and public site from Deskora to **NimbMote**.
- The Android application ID and local pairing storage remain unchanged, so this
  installs as a normal update and keeps an existing pairing.
- The Windows installer keeps its existing install identity for an in-place
  update, replaces the known Deskora shortcuts/startup entry with NimbMote, and
  preserves local pairing data unless the user explicitly requests removal.

## Verification

- Protocol fixture validation passed.
- Android debug APK built as versionName `0.9.0` / versionCode `9001` and its
  APK Signature Scheme v2 signature was verified.
- Windows x64 self-contained agent published successfully.
- Inno Setup 6.7.3 compiled the Windows installer successfully.

## SHA-256

- `NimbMote-v0.9.0-debug.apk`
  `E342C5FD6650534AAAB7BAD7BA392AEACC920402F51F12FCAC2146E082C92A2E`
- `NimbMote-Setup-v0.9.0.exe`
  `4189A5075F578A4B9DA73DDEFFDB5FAA405C335C4A7C5674589FBEF4AE9123A5`
