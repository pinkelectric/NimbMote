# Bentley Remote v0.2.1 — test build

## Fixed

- Android now releases the MediaSession, Media3 notification and One UI remote
  VolumeProvider after a confirmed Windows outage. An explicit disconnect has a
  15-second continuous-reconnect grace period; a 35-second authenticated-heartbeat
  timeout is immediately treated as confirmed offline. A newly authenticated PC
  recreates the remote controls without losing the existing pair.
- For an Android-initiated Shutdown or Restart, a positive `command.result` from
  Windows immediately removes the media card and remote-volume control. Rejection,
  timeout or no confirmation keeps the usual offline path; pairing is retained.
- Windows GSMTC now subscribes to current-session, metadata, playback and timeline
  events. Edge metadata revisions invalidate stale artwork, so an older closed tab's
  thumbnail cannot overwrite the current tab.
- The main power controls use official Compose Material vector icons plus labels for
  «Выключить» and «Перезагрузить». Сон, Заблокировать and the rare reverse fallback
  are in the overflow menu. The fallback is now called «Дополнительные настройки
  подключения» and is no longer displayed on the normal main screen.

## Compatibility note

- User-reported manual observation: Bentley Remote operated stably on a Galaxy S6
  with an S8-based firmware. This is not formal device certification; OS/build details
  are pending.

## Verification completed

- Android Debug APK: compile, unit tests and `assembleDebug` passed (versionName
  `0.2.1`, versionCode `2002`). APK Signature Scheme v2 verified; signer certificate
  SHA-256 matches v0.1.1 (`34cb31a3…2c7629`).
- Windows Agent: Release build/publish passed. Safe console tests passed: power
  allowlist/fake controller, paired/bootstrap discovery and replay rejection,
  pairing crypto, directed broadcast, loopback responder, artwork-revision guard.
  No real power action was run.
- Protocol validation and Git whitespace checks passed.

## Still requires hardware confirmation

Run the focused v0.2.1 checklist in `TESTING.md`: physical/offline PC shutdown,
recovery after restart, rejected power action, Edge tab artwork switch, Material-icon
power UI and overflow connection settings on Galaxy/Windows hardware. The desktop
screenshot card is explicitly deferred to v0.3+ and is not in this build.

## SHA-256

- `BentleyRemote-v0.2.1-debug.apk`: `8AF2DDD5A28D62010F3D569E5E97FA967C23BB9AA9ABBEF9FCC52AD1678E00FD`
- `BentleyRemote-v0.2.1-windows-x64.zip`: `378E2809B374D55D8EBC21EC248BBF2D152706E9E4A28CC5C3820E0FFAD4F45B`
