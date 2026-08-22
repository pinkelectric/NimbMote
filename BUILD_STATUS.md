# Build status — v0.2.7 — 2026-08-22

- Android: release build and unit tests completed for the vector media controls
  and paired-only BOOT_COMPLETED/MY_PACKAGE_REPLACED receiver. The installable
  APK is signed with the established Android debug test certificate (SHA-256:
  34cb31a3fb393e034948eba1ba007215ea8025a4759fc8bf4dff2e3cbd2c7629).
- Windows and Setup are intentionally unchanged in this Android-only patch;
  their current installable artifact remains v0.2.6.
- Android 15+ does not permit the existing `mediaPlayback` MediaSessionService
  to start from BOOT_COMPLETED. v0.2.7 instead starts a separately declared
  `connectedDevice` foreground service for its real LAN-device connection,
  using the required `CHANGE_WIFI_STATE` manifest prerequisite. It shows a
  status notification and restores the reconnect loop; the Media3 player card
  is restored only after the user opens the app. Hardware verification on One
  UI remains required because a Force Stop or battery-restricted app can block
  boot delivery.
