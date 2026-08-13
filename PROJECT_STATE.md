# Bentley Remote — Project State

## Current stable-test target

- Work in progress: `v0.2.1` patch release from committed/tagged `v0.2.0`.
- v0.2.0 remains unchanged at tag `v0.2.0`; its release directory contains only
  its APK, ZIP and CHANGELOG.

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

- The v0.2.1 shutdown/offline, recovery, Edge-tab-artwork and advanced-settings
  scenarios in `TESTING.md` require a real Galaxy and Windows PC.
