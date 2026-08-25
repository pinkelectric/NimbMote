# Deskora v0.7.0

## Public identity

- User-facing product name changed from Bentley Remote to **Deskora** on Android
  and in the Windows agent, pairing dialog, installer, Start menu and desktop
  shortcut.
- Added an original Deskora launcher mark: two connected device frames on a
  dark-plum tile. The Windows tray uses the matching mark.
- Fresh Windows installs default to `C:\Program Files\Deskora`; the installer
  removes only its known legacy Bentley Remote autostart value before registering
  the Deskora value, so it does not create two agents at startup.
- Existing Android pairing and Windows configuration are deliberately retained
  for this transition. The technical Android package name will change in a
  dedicated pre-Google-Play migration release.

## Verification

- Android unit tests: passed.
- Windows agent test harness: passed.
- Protocol fixtures: passed.
- APK: verified with Android APK Signature Scheme v2, one debug signer.
- APK manifest: application label `Deskora`; launcher resource
  `@mipmap/ic_launcher` verified.
- Windows installer: compiled successfully with Inno Setup 6.7.3.

## SHA-256

- `Deskora-v0.7.0-debug.apk`:
  `9591DE4D355714C828FD2EA84E2D566F7FAB61C1B90DC2BFCA00A66109921B97`
- `Deskora-Setup-v0.7.0.exe`:
  `64E36F54F04BC5DC328596777B9BE6A6D8D456D5848C3EBBDCB8AFA3C287AD92`
