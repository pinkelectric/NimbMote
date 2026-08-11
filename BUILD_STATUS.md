# Build status — 2026-08-11

## Binary builds completed

- Android `assembleDebug`: **PASS** (36 tasks, Gradle 8.11.1, API 35).
- APK signature verification: **PASS** (Android debug certificate, APK
  Signature Scheme v2).
- Windows `dotnet publish`: **PASS** (.NET SDK 8.0.423, Release, win-x64,
  self-contained).
- Protocol examples and HMAC-SHA256 vector: **PASS**.
- Package integrity: **PASS** (477 file hashes and 478 readable ZIP entries).

Binary package:

- `outputs/BentleyRemote-binaries.zip`
- SHA-256:
  `4be7150d0706a803d825dbf8036a3d416beb1c117d0c65ddea12e139c22fdbbb`

The package contains `android/BentleyRemote-debug.apk`, the complete
`windows-x64` self-contained agent folder, `BUILD_REPORT.md`, `README.md`,
`TESTING.md`, and `SHA256SUMS.txt`.

## Portable build environment

All downloaded SDKs and caches remain below the workspace `work` directory;
no system-wide SDK installation was used. The build scripts now use direct
official download endpoints, resumable retries, SHA-256 verification, a
single non-daemon Android build, and a `-PackageOnly` recovery mode.

Hardware-only behavior still requires the checks in `TESTING.md` on Windows 11
and the Galaxy A56 hotspot.
