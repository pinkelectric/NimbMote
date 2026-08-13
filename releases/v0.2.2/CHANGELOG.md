# Bentley Remote v0.2.2 — test build

## Fixed

- Windows paired reconnect no longer waits for the optional Global System Media
  Transport Controls (GSMTC) API. At Windows autostart, the LAN connection hub
  starts immediately and continues the existing paired cascade: last-known
  endpoint, gateway fast path, then authenticated UDP broadcast discovery.
- A delayed Windows media broker now retries independently with bounded waits and
  clear tray/debug status. It can no longer terminate the reconnect service before
  Wi-Fi becomes usable.
- Paired LAN discovery still uses signed UDP broadcast (directed subnet broadcasts
  plus limited broadcast after an IPv4 interface is ready); no TCP `/24` scan was
  added. Galaxy-hotspot gateway behaviour is unchanged.

## Verification completed

- Android Debug APK: compile, unit tests and `assembleDebug` passed (versionName
  `0.2.2`, versionCode `2003`). APK Signature Scheme v2 verified; signer
  certificate SHA-256 matches v0.1.1/v0.2.1 (`34cb31a3…2c7629`).
- Windows Agent: Release build and self-contained win-x64 publish passed. Safe
  console tests passed: power allowlist/fake controller, paired/bootstrap discovery
  and replay rejection, pairing crypto, directed broadcast, loopback responder,
  artwork revision and bounded startup-readiness retry. No real power action ran.
- Protocol validation, Git whitespace checks and ZIP extraction (473 files) passed.

## Still requires hardware confirmation

On a Galaxy and Windows PC connected as clients to the same third-party Wi-Fi or
hotspot, reboot Windows and let the agent start automatically. Confirm that the
authenticated session restores without restarting the agent or entering an IP;
then repeat the Galaxy-hotspot regression. See scenario D in `TESTING.md`.

## SHA-256

- `BentleyRemote-v0.2.2-debug.apk`: `B4E66F5E88507B322986C99CAE499BD3A79209019835997C48C650F11712FF27`
- `BentleyRemote-v0.2.2-windows-x64.zip`: `5AD5211BA02FE8F4D1C69422661BE05E4BBA5D541DBE7ADCC1C515F6965444E5`
