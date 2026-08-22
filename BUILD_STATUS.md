# Build status - v0.2.8 - 2026-08-22

- Android: release build and unit tests completed for the removal of the v0.2.7
  boot-only reconnect path. The installable APK is signed with the established
  Android debug test certificate (SHA-256:
  `34cb31a3fb393e034948eba1ba007215ea8025a4759fc8bf4dff2e3cbd2c7629`).
- This patch removes the boot receiver, connectedDevice foreground service,
  notification channel/strings and boot-service handoff code. No Bentley
  notification is created after phone reboot; manual launch retains the normal
  MediaSession connection path.
- Windows and Setup are intentionally unchanged; their current installable
  artifact remains v0.2.6.

- APK SHA-256: `22C7D38B1779E504B7B85ED2EBCCB8E81821D021A1ABB3E7398F4773D361AE55`.
