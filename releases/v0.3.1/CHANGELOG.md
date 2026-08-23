# Bentley Remote v0.3.1

## Changed

- Fixed Android validation of encrypted desktop previews: it now uses the exact
  line-feed format used by the unchanged Windows agent. Valid previews were
  previously rejected and shown as «Рабочий стол недоступен».
- Compact single-line Shutdown and Restart button labels prevent the Russian
  labels from breaking into two lines.
- This is an Android-only compatible patch. Keep the installed Windows Setup
  v0.3.0; no Windows reinstall is needed.

## Verification

- Android `compileReleaseKotlin`, unit tests and release package passed
  (9 tests).
- APK signature verified with v2 and v3. The Windows v0.3.0 Setup is unchanged.

## SHA-256

- `BentleyRemote-v0.3.1-release.apk`:
  `1EC654CEBF50A61934D60CAA3335664EE58E9348DE6A1AF731A54BFCE12E4390`
- Reused `BentleyRemote-Setup-v0.3.0.exe`:
  `C6101B97EEBE126B953EF04083CCF0E3D035C4978D1F61E5FC5D7FBEAFA85A56`
