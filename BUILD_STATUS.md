# Build status - v0.3.0 - verified release - 2026-08-23

- Android: `:app:compileReleaseKotlin`, `:app:testDebugUnitTest` and
  `:app:assembleRelease` passed with local JDK 17 and Android SDK 35.
- Android unit tests: RemotePlayerVolume, TimelinePredictor,
  RemoteOfflinePolicy and PowerUiStatic passed (8 tests total).
- Windows: Release build and self-contained `win-x64` publish passed; the native
  Inno Setup package compiled successfully. The installer was not executed.
- Signed APK verified with v2 and v3 using the established Android debug test
  certificate SHA-256 `34cb31a3fb393e034948eba1ba007215ea8025a4759fc8bf4dff2e3cbd2c7629`.
- APK SHA-256: `5499741678DD154533135B4C97D0821A661A847D1B4BEF0F55CFD75EC9DED154`.
- Setup SHA-256: `C6101B97EEBE126B953EF04083CCF0E3D035C4978D1F61E5FC5D7FBEAFA85A56`.
