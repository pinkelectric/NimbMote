# Bentley Remote v0.2.7 — Android patch

## Changed

- Media controls use Material vector icons; Pause is no longer rendered as a
  system emoji.
- A previously paired installation can restore its LAN reconnect loop after a
  normal device boot or app update. The receiver starts a dedicated
  `connectedDevice` foreground service with a persistent Bentley status
  notification. It does not create the Media3 media card from boot.
- The normal activity/service path takes over when the user opens Bentley
  Remote and then creates the existing media integration.

## Android 15+ behaviour

`mediaPlayback` foreground services cannot be started directly from
`BOOT_COMPLETED`. Therefore the boot receiver runs only for an existing paired
installation and starts the separately declared `connectedDevice` reconnect
service. A clean/unpaired install does nothing at boot. Android Force Stop,
notification denial, or restrictive battery policy can still prevent the
system from showing or delivering background work.

## Verification

- `:app:compileReleaseKotlin`, `:app:testDebugUnitTest`, and
  `:app:assembleRelease`: passed.
- Unit tests: RemotePlayerVolume, TimelinePredictor, RemoteOfflinePolicy and
  PowerUiStatic.
- APK signature: verified with v2 and v3; established Android Debug test
  certificate SHA-256:
  `34cb31a3fb393e034948eba1ba007215ea8025a4759fc8bf4dff2e3cbd2c7629`.
- Windows agent and installer are intentionally unchanged; retain v0.2.6.

## SHA-256

- `BentleyRemote-v0.2.7-release.apk`:
  `6ACF804FC74079F3F5F3264163081EF8EFD1B0811F991D80312069828AAA365E`
