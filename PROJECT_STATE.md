# Bentley Remote — Project State

## Current stable-test target

- Latest stable-test release: `v0.2.4`, following committed/tagged `v0.2.3`.
- Previous releases remain unchanged at their tags; each release directory contains
  only its APK, Windows Setup EXE and CHANGELOG.

## v0.2.2 scope

- Windows paired LAN reconnect now starts independently of optional GSMTC startup.
  On a cold Windows sign-in, a delayed/unavailable global media API no longer stops
  last-known, gateway and authenticated UDP broadcast retries. Tray diagnostics show
  that distinction and media initialization retries with a bounded delay.

## v0.2.4 scope

- Windows agent uses a native interactive Inno Setup package. It installs or
  updates per-machine in `C:\Program Files\Bentley Remote` with one normal UAC
  elevation, replaces only known Bentley Remote autostart entries, and starts
  the current agent. DPAPI pairing state remains in `%LOCALAPPDATA%\BentleyRemote`.
- Tray About/diagnostics reports the binary version and actual executable path.
- The desktop screenshot card remains deferred to v0.3+; v0.2.4 is a compatible
  install/update reliability patch.

## v0.2.1 scope

- Android releases the MediaSession, Media3 notification and remote VolumeProvider
  after a confirmed Windows outage: 15 seconds of continuous explicit disconnect,
  or immediately after a 35-second authenticated-heartbeat timeout.
- A Windows-confirmed Android-initiated Shutdown or Restart is an additional fast
  path: it immediately removes those remote controls without forgetting pairing.
- Edge GSMTC metadata/artwork changes subscribe to GSMTC events and invalidate a
  stale thumbnail revision before a new one can be emitted.
- Main UI shows Material-icon buttons only for Shutdown and Restart. Sleep, Lock
  and the rare reverse-connection fallback are inside the existing overflow menu.

## Manual compatibility observations

- Galaxy A56 / One UI 8.5: v0.1.1 media behavior was hardware-confirmed by the user.
- Galaxy S6 with an S8-based firmware: user reports stable operation. This is not
  a certification and device/build details are still pending.

## Backlog (not included in v0.2.1)

- Desktop screenshot card in Android. Defer to v0.3+ design/scope; no screen capture,
  streaming or new permissions are included in this patch release.

## Next hardware checks

- The v0.2.2 Windows-autostart-after-reboot scenario on a third-party hotspot, plus
  existing shutdown/offline, recovery, Edge-tab-artwork and advanced-settings checks
  in `TESTING.md`, require real Galaxy and Windows hardware.
