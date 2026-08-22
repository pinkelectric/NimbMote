# Bentley Remote v0.2.8 — Android UX rollback

## Changed

- Removed the v0.2.7 boot receiver and boot-only LAN reconnect service.
- Bentley Remote does not launch itself after phone restart and creates no
  standalone Bentley notification at boot.
- Manual app launch retains saved pairing, normal LAN reconnect and the existing
  MediaSession media card during Windows media control.
- Windows agent and installer remain v0.2.6.

## Verification

- `:app:compileReleaseKotlin`, `:app:testDebugUnitTest` and
  `:app:assembleRelease`: passed using local JDK 17 and Android SDK 35.
- Unit tests: RemotePlayerVolume, TimelinePredictor, RemoteOfflinePolicy and
  PowerUiStatic.
- Static check: no Boot receiver/service, `BOOT_COMPLETED`, `connectedDevice`,
  `RECEIVE_BOOT_COMPLETED` or `CHANGE_WIFI_STATE` remains in the Android source
  or manifest.
- APK signature verified with v2 and v3. Established debug test certificate:
  `34cb31a3fb393e034948eba1ba007215ea8025a4759fc8bf4dff2e3cbd2c7629`.

## SHA-256

- `BentleyRemote-v0.2.8-release.apk`:
  `22C7D38B1779E504B7B85ED2EBCCB8E81821D021A1ABB3E7398F4773D361AE55`
