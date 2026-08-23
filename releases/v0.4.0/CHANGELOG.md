# Bentley Remote v0.4.0

## Added

- A paired Bentley phone can receive one locally staged Android test APK from
  this Windows account. The temporary card appears only when a package is ready.
- APK delivery uses the existing paired connection, AES-256-GCM encryption for
  every chunk, a 200 MiB cap, strict ordering and SHA-256 verification before
  Android exposes its normal user-confirmed install action.
- The global Codex skill `android-test-delivery` lets Bentley, TouchGrass and
  other local Android project chats stage their verified APK through one shared
  local queue. The queue keeps only the latest APK and never uploads it.

## Verification

- Android compile, unit tests and release packaging passed (9 tests).
- Windows Release build, self-contained publish and Inno Setup package passed.
- The global staging script was run against this release APK and verified its
  resulting local SHA-256 queue record.

## SHA-256

- `BentleyRemote-v0.4.0-release.apk`:
  `FF63E23E8C2A382F448E4C6CDD73E238469A7D209BF84B997D14824CE22F7790`
- `BentleyRemote-Setup-v0.4.0.exe`:
  `DF549CBFF9074634084B42CE9AC8D4E0963ACA630CC991565C9D725EE7F1F489`
