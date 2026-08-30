# Deskora v0.7.3

## Windows icon and shortcut repair

- Embedded the Deskora monitor-and-phone mark into the Windows agent executable.
- Assigned that icon explicitly to the Deskora Start-menu and desktop shortcuts.
- On upgrade, removes only the two obsolete Bentley Remote shortcuts that were left by older installers.
- The Windows installer itself now also uses the Deskora icon.
- No Android functionality or pairing protocol changed in this version.

## Verification

- Windows agent test harness: passed.
- Windows executable: Windows successfully extracted the embedded `32×32` application icon.
- Windows installer: compiled successfully with Inno Setup 6.7.3.
- Android unit tests: passed; the APK is included only to keep the release package version-consistent and is not staged for the phone.
- APK: verified with Android APK Signature Scheme v2.

## SHA-256

- `Deskora-v0.7.3-debug.apk`:
  `D62B6FD8E38CCF3B0D14722151AF1FAA0496A133E5AD203122686173CB05250D`
- `Deskora-Setup-v0.7.3.exe`:
  `ACF08E5D92F75856731EB9EB0A423376E3217B9B347F9B0F705DB4CB367F5504`
