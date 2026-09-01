# Deskora v0.8.0 — Quick Settings Play/Pause tile

## Added

- Add **Deskora Play/Pause** to Android Quick Settings and control Windows media even after Android removes its media card from the notification shade.
- A tile tap briefly waits for the paired Windows agent to reconnect rather than discarding the command when Android has reclaimed the app.
- Android 13 and newer can show the system add-tile prompt directly from Deskora. On Android 8–12, add the tile from the Quick Settings edit screen.

## Scope and verification

- This feature is Android-only. The Windows-agent protocol and normal behaviour are unchanged; the matching Setup EXE is included for release-version consistency and does not need to be installed for this Android update.
- Android unit tests passed; debug APK built as versionName `0.8.0` / versionCode `8001`; APK Signature Scheme v2 verified.
- Windows agent tests passed; `win-x64` publish and Inno Setup packaging completed successfully.

## SHA-256

- `Deskora-v0.8.0-debug.apk`:
  `7C5639749457450D46761B271DCB973666C29C29768DEFE5EBA874B08F4A1E49`
- `Deskora-Setup-v0.8.0.exe`:
  `DD298E37BC268D41335FA4097A45F3102FFECFE489533DF73A77D6F5C0A3A506`
