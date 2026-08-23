# Build status - v0.3.1 - verified Android patch - 2026-08-23

- Android: `:app:compileReleaseKotlin`, `:app:testDebugUnitTest` and
  `:app:assembleRelease` passed with local JDK 17 and Android SDK 35.
- Android unit tests: RemotePlayerVolume, TimelinePredictor,
  RemoteOfflinePolicy, PowerUiStatic and DesktopPreviewCrypto passed (9 tests total).
- Windows: the v0.3.0 agent/Setup is unchanged; its Release build passes for
  this v0.3.1 compatibility check (one existing duplicate-using warning,
  no errors). The installer is not reissued.
- Signed APK verified with v2 and v3 using the established Android debug test
  certificate SHA-256 `34cb31a3fb393e034948eba1ba007215ea8025a4759fc8bf4dff2e3cbd2c7629`.
- APK SHA-256: `1EC654CEBF50A61934D60CAA3335664EE58E9348DE6A1AF731A54BFCE12E4390`.
- Reused v0.3.0 Setup SHA-256: `C6101B97EEBE126B953EF04083CCF0E3D035C4978D1F61E5FC5D7FBEAFA85A56`.
