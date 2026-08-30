# Deskora v0.7.2

## Final polish before the public release

- Fixed the test-package card so a staged Deskora build is offered only when it is newer than the installed app. The card no longer reappears after installing that same version.
- Replaced the persistent `Windows accepted the action` acknowledgement beneath the power buttons with quiet success; failures are still reported.
- Added a first-run connection flow on Android: install the Windows agent, read its six-digit code, and enter it while both devices are on the same Wi-Fi network.
- A fresh Deskora Agent installation now opens the pairing-code window automatically. Updating an existing agent starts quietly as before.
- Kept six-digit pairing as the universal path; QR pairing can be added later alongside it without making a camera mandatory.

## Verification

- Android unit tests: passed (11 tests).
- Windows agent test harness: passed.
- APK: verified with Android APK Signature Scheme v2.
- APK manifest: `Deskora`, version `0.7.2` (code `7003`).
- Windows installer: compiled successfully with Inno Setup 6.7.3.

## SHA-256

- `Deskora-v0.7.2-debug.apk`:
  `F13831ED788F90DF5349B0C2BF0CB02CAE965049CD8D4B365F8C68C1D38FB683`
- `Deskora-Setup-v0.7.2.exe`:
  `7E6CE5C1A99D0F2B7128FA3D107E26557BD71F2EB0CCE553FCC9276FAC9C4038`
