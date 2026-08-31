# Deskora v0.7.7 — Android reconnect recovery and new icon

## Android reconnect recovery

- The LAN discovery listener now recreates its UDP socket after a network-level failure instead of remaining alive but unable to answer the Windows agent.
- The active transport periodically confirms that discovery is listening, so a rebooted Windows agent can find the paired phone again.
- Returning to the app reasserts the same local service safely. This does not add phone-boot autostart or a permanent Deskora notification.

## Deskora mark

- Android and Windows now use the new Deskora mark: a vertical phone on the left and a larger horizontal computer screen on the right.
- The Windows executable, installer, Start-menu shortcut, desktop shortcut and runtime tray icon all receive the updated mark.

## Verification

- Android debug APK built as versionName `0.7.7` / versionCode `7008` and its APK Signature Scheme v2 signature was verified against the established debug certificate.
- Windows agent tests passed; self-contained Windows `win-x64` publish and the Inno Setup package completed successfully.

## SHA-256

- `Deskora-v0.7.7-debug.apk`:
  `5773B484AC2948143B8148A36B3C39A61ECD58D8AAD91AA1ABA7A4201828C347`
- `Deskora-Setup-v0.7.7.exe`:
  `2B16BDA4E2F14555B37B1FD2419499EC0DCBB77049A975ABB5736234A6AAC333`
