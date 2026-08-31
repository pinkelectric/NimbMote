# Deskora v0.7.4

## First-run and uninstall fixes

- Fixed the first-run Android layout: connection settings and pairing reset are now separate full-width buttons, so no label can collapse into a narrow column.
- Shortened the first-run helper text; public-download wording is deferred until the website has a real link.
- The Windows uninstaller now asks whether to remove saved pairing data:
  - **Yes** removes Deskora completely, including the paired phone and secret, so a new installation behaves as a first install.
  - **No** removes only the agent and preserves pairing for a later reinstall.

## Verification

- Android unit tests: passed.
- APK: verified with Android APK Signature Scheme v2.
- Windows agent test harness: passed.
- Windows installer: compiled successfully with Inno Setup 6.7.3.

## SHA-256

- `Deskora-v0.7.4-debug.apk`:
  `0BCD7D75013ED7F042363B8BF968C179041D9D98D9808059748A3015BFF53E45`
- `Deskora-Setup-v0.7.4.exe`:
  `C85BA0FBC84C089193214CE338F384D6A0A23924462FA3108F2AA980EB39FBAA`
