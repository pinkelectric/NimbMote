# Deskora v0.7.6 — Windows hotfix

## Pairing-code window

- Rebuilt the pairing window with DPI-aware table layout, dedicated button space, and a wider client area. The Close button can no longer be clipped at higher Windows display scaling.
- The window now checks the pairing state every 250 ms and closes itself as soon as Android successfully connects.

## Scope and verification

- Windows-only hotfix; Android app was not rebuilt or sent to the phone.
- Windows agent test harness: passed.
- Windows installer: compiled successfully with Inno Setup 6.7.3.

## SHA-256

- `Deskora-Setup-v0.7.6.exe`:
  `6DCC595D955AF15A2441A13881A81A431336102BC6D67F8C7B45B22DE35B7F87`
