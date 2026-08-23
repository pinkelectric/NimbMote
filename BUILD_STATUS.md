# Build status - v0.4.0 - verified test-delivery release - 2026-08-23

- Android: `:app:compileReleaseKotlin`, `:app:testDebugUnitTest` and
  `:app:assembleRelease` passed with local JDK 17 and Android SDK 35.
- Android unit tests: RemotePlayerVolume, TimelinePredictor,
  RemoteOfflinePolicy, PowerUiStatic and DesktopPreviewCrypto passed (9 tests total).
- Windows: Release build and self-contained `win-x64` publish passed; the native
  Inno Setup package compiled successfully.
- Signed APK verified with v2 and v3 using the established Android debug test
  certificate SHA-256 `34cb31a3fb393e034948eba1ba007215ea8025a4759fc8bf4dff2e3cbd2c7629`.
- APK SHA-256: `FF63E23E8C2A382F448E4C6CDD73E238469A7D209BF84B997D14824CE22F7790`.
- Setup SHA-256: `DF549CBFF9074634084B42CE9AC8D4E0963ACA630CC991565C9D725EE7F1F489`.
