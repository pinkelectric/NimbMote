# Build status — v0.2.4 — 2026-08-13

## Versioned build completed

- Canonical `VERSION`: `0.2.4`.
- Android: `compileDebugKotlin`, `testDebugUnitTest` and `assembleDebug` passed
  offline with local JDK 17 and SDK 35. The APK is versionName `0.2.4`,
  versionCode `2005`; its v2 debug signer SHA-256 is
  `34cb31a3fb393e034948eba1ba007215ea8025a4759fc8bf4dff2e3cbd2c7629`,
  unchanged from prior test releases.
- Windows: Release build, safe console tests and self-contained `win-x64`
  publish passed. Tests include pairing/discovery cryptography, fake power
  dispatch and Program Files/startup layout intent; no system power command,
  installation or registry modification was run.
- Native installer: Inno Setup 6.7.3 successfully compiled
  `BentleyRemote-Setup-v0.2.4.exe`. Static checks confirm its product version,
  Program Files default, elevated installation, HKLM single Run value and
  narrowly scoped removal of only the two legacy Bentley HKCU Run values.
  The EXE was not executed on this PC.
- Protocol validation and Git whitespace checks passed.

## Stable test release

`releases/v0.2.4/` contains exactly the debug APK, Windows Setup EXE and
CHANGELOG. The release commit is annotated tag `v0.2.4`. Manual install/update,
reboot-autostart and hardware connection checks remain in `TESTING.md`.
