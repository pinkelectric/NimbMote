# NimbMote v0.10.1 — media card repair

## Fixed

- Removed the persistent **NimbMote is ready** connection notification added in
  v0.10.0. It replaced the Android MediaSession card, so the Windows player
  could disappear from the notification shade.
- Retired that notification channel during upgrade, including any remaining
  v0.10.0 status card.
- The only NimbMote notification is again the Windows media card, shown when
  Windows has an active media session.
- A newly created MediaSession now seeds itself from the latest local state,
  avoiding a reconnect ordering race that could leave the media card empty.

## Retained from v0.10.0

- Local-only connection diagnostics.
- Quick Settings long press opens NimbMote.
- An offline Quick Settings tap reconnects without queuing an unwanted media
  toggle.

## Verification

- Android unit tests and protocol fixture validation passed.
- Android debug APK built as versionName `0.10.1` / versionCode `10002`; APK
  Signature Scheme v2 was verified.
- Windows x64 self-contained agent built successfully as `0.10.1`.
- Inno Setup 6.7.3 compiled the Windows installer successfully.

## SHA-256

- `NimbMote-v0.10.1-debug.apk`
  `D3808B854D05D91828BDA268DC3A5965A97F0A37789805FF2A569191E764CDF2`
- `NimbMote-Setup-v0.10.1.exe`
  `985471568DA62C25E6C5FC2E827150618287768B8D00A74313E37EDA219CE435`
