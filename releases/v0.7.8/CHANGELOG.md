# Deskora v0.7.8 — Android update-card fix

## Fixed

- A Deskora test-package label may now include a short note after its semantic version, such as `Deskora v0.7.8 — update-card fix`.
- That package is still recognised as Deskora itself, so the test-build card disappears immediately after that exact version is installed instead of incorrectly remaining visible as an update.

## Scope and verification

- The paired protocol and Windows-agent behaviour are unchanged; do not reinstall the Windows agent for this update. A matching v0.7.8 Setup EXE is included only to keep both platforms on the same release number.
- Android unit tests passed, including the version-label case with a release note.
- Android debug APK built as versionName `0.7.8` / versionCode `7009`; APK Signature Scheme v2 verified.
- Windows agent tests passed; self-contained `win-x64` publish and Inno Setup packaging completed successfully.

## SHA-256

- `Deskora-v0.7.8-debug.apk`:
  `6A6416023C1B3972BEEDB246B1567B7D98A239EEA1541E5B8D9A224111A70252`
- `Deskora-Setup-v0.7.8.exe`:
  `0CC9969C68AB6A71E548DB885EA2DD081E47CB3CF75963520C5EBCC9A575F18A`
