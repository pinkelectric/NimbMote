# Bentley Remote — Project State

## Current stable-test target

- Stable-test release: `v0.7.7`. Android main screen starts with the paired
  computer card and an on-demand encrypted primary-desktop preview, then media
  and Windows volume. Device locale chooses English/Russian strings. Multi-phone
  pairing remains a v0.4+ backlog item.

## v0.7.7 scope

- Android's paired-LAN listener recreates its UDP socket after a network-level
  failure, and the live transport periodically confirms it is still listening.
  This repairs the case where a Windows reboot left Android open but unable to
  answer a new discovery probe until the user force-stopped the app.
- Returning to the Android Activity reasserts the existing local service. This
  is not phone-boot autostart and does not restore the rejected permanent
  Deskora status notification.
- Android and Windows use the new Deskora two-device mark: vertical phone on
  the left and horizontal desktop display on the right. The Windows executable,
  installer, Start-menu and desktop shortcuts share the same source icon.

## v0.4.0 scope

- A reusable local Android test-delivery queue accepts one staged APK from any
  Codex project. The Windows agent notices the queue and offers the package only
  to its currently paired phone.
- APK chunks are AES-256-GCM encrypted with the existing pairing secret, bounded
  to 200 MiB and verified against the staged SHA-256 before Android exposes an
  ordinary user-approved Install action.
- The global `android-test-delivery` skill provides the stage script for Bentley,
  TouchGrass and future local Android projects. This is not cloud upload, silent
  installation or an arbitrary-file sender.

## v0.4.1 scope

- The computer-card image now comes from the active Windows wallpaper file, not
  a screenshot of the interactive display. Open windows, taskbar and overlays
  are never captured for this feature.
- This is an installer-only Windows patch. Android v0.4.0 is retained because
  the paired protocol and phone UI are unchanged.

## v0.3.1 scope

- Android's AES-GCM additional authenticated data for a desktop preview now uses
  the protocol's real line-feed bytes, matching the existing Windows agent. This
  restores accepted authenticated desktop previews; the v0.3.0 Android client
  encoded the visible `\\n` characters instead, so valid preview tags were rejected.
- Shutdown and Restart labels use a single line with appropriately compact
  padding and icon size on a 1080px Galaxy display.
- This is an Android-only compatible patch. The Windows agent/Setup remains
  `v0.3.0`; neither reinstall nor repair of Windows is required.

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

## v0.2.5 scope

- GSMTC MediaPropertiesChanged advances artwork generation even for identical or
  empty Edge metadata, clears outgoing artwork before async loading, and rejects
  late results from older generations.
- Android main-screen progress is predicted locally from the latest monotonic
  timeline snapshot while playing; pause/seek/new snapshot rebase it immediately.
- Native Setup exposes directory selection, a Start-menu shortcut and an enabled
  desktop-shortcut task while keeping autostart pointed to the selected `{app}`.

## v0.2.6 scope

- Start-menu icon uses the explicit common Programs folder instead of `{group}`;
  it is unconditional and independent of the optional desktop task. Android is
  unchanged, so this installer-only release does not duplicate an APK.

## v0.2.7 scope

- Android media controls use Material vector icons for Previous, Play/Pause and
  Next. In particular, Pause is no longer the differently coloured system emoji.
- A saved paired Android installation registers for normal boot and APK-update
  broadcasts. On Android 15+ it starts a dedicated `connectedDevice` foreground
  service (the saved authenticated LAN connection to the paired PC), which owns
  a persistent Bentley status notification and reconnect loop without opening
  the Activity. This is legal from `BOOT_COMPLETED` with the declared
  `CHANGE_WIFI_STATE` prerequisite.
- The boot service deliberately does not create a Media3 media-card:
  Android 15+ prohibits a boot receiver from starting the `mediaPlayback`
  foreground service. Tapping the status notification, or otherwise opening
  Bentley Remote, hands off to the normal MediaSessionService.

## v0.2.8 scope

- User-requested UX rollback of the v0.2.7 Android boot mechanism. The boot
  receiver, `connectedDevice` foreground service, related manifest permissions,
  strings and handoff ownership code are removed.
- After phone reboot Bentley Remote does not start itself and creates no Bentley
  notification. Opening the app manually retains the established connection,
  MediaSession and standard media card behaviour.

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

- Multiple simultaneous paired phones/tablets for one PC (v0.4+); v0.3 remains
  deliberately one authenticated paired endpoint.

## Next hardware checks

- The v0.2.2 Windows-autostart-after-reboot scenario on a third-party hotspot, plus
  existing shutdown/offline, recovery, Edge-tab-artwork and advanced-settings checks
  in `TESTING.md`, require real Galaxy and Windows hardware.
- The v0.2.8 manual-launch regression needs a real Galaxy check: after a normal
  reboot Bentley must create no standalone notification; after manual opening,
  saved pairing, reconnect and the usual Media3 card must work as before.
