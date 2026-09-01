# Deskora v0.7.9 — paused media controls stay available

## Fixed

- The Android media card now remains available after pausing Windows media, so playback can be resumed from the phone's notification shade without reopening Deskora.
- The fix holds only a real paused media session.  Deskora still does not create a separate persistent status notification when no Windows media session exists.

## Scope and verification

- Windows-agent protocol and behaviour are unchanged; the included matching Setup EXE is only for release-version consistency and does not need to be installed for this Android fix.
- Android unit tests passed, including the paused-session notification policy.
- Android debug APK built as versionName `0.7.9` / versionCode `7010`; APK Signature Scheme v2 verified.
- Windows agent tests passed; self-contained `win-x64` publish and Inno Setup packaging completed successfully.

## SHA-256

- `Deskora-v0.7.9-debug.apk`:
  `F04A8A79D382D4C9F726FF1CF39277E582D3C928B390A095F9D3DE1A668EDF54`
- `Deskora-Setup-v0.7.9.exe`:
  `4D440B89755D01CE5CA3363E4BD37AEA57688B77879F8A754124275B8C2BF8F6`
