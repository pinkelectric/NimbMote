# Build status — v0.1.0 — 2026-08-11

## Versioned builds completed

- Canonical version: `VERSION` = `0.1.0`.
- Android `assembleDebug`: **PASS** (36 tasks, versionCode 1001, versionName
  0.1.0, APK Signature Scheme v2).
- Windows `dotnet publish`: **PASS** (Release, win-x64, self-contained,
  Assembly/File/Product version 0.1.0.0 / 0.1.0.0 / 0.1.0).
- Protocol examples and HMAC-SHA256 vector: **PASS**.
- Binary package checksum verification: **PASS** (477 files).

## Stable test release

`releases/v0.1.0/` contains exactly:

- `BentleyRemote-v0.1.0-debug.apk`;
- `BentleyRemote-v0.1.0-windows-x64.zip`;
- `CHANGELOG.md` with the artifact SHA-256 values.

The release directory contains no source snapshot. Source history is stored in
Git. The release commit is tagged `v0.1.0` after all repository checks pass.

All portable SDKs and caches remain outside the repository in the workspace
`work` directory. Hardware behavior still requires the checklist in
`TESTING.md` on Windows 11 and the Galaxy A56 hotspot.
