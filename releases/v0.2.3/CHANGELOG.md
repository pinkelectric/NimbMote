# Bentley Remote v0.2.3 — test build

## Fixed: install and update path for Windows agent

- Added a per-user managed location: `%LOCALAPPDATA%\BentleyRemote\Agent`.
- Windows ZIP now includes `Install-Or-Update.ps1` beside `BentleyRemote.Agent.exe`.
  It stops only the Bentley agent, stages and replaces its managed files, writes one
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Bentley Remote` value for
  the managed EXE, removes only the known legacy Bentley value, then starts the
  updated agent.
- Existing `%LOCALAPPDATA%\BentleyRemote\agent-config.json` is outside the
  replaceable `Agent` folder, so the current-user DPAPI pairing state is preserved.
- Tray now has **About / diagnostics…** with the running version and actual EXE
  path. The tray autostart toggle refuses to register a portable/unpacked copy.

## Why v0.2.3

This is a compatible patch: it fixes the deployment/upgrade mechanism without
changing the pairing, discovery or command protocol. It prevents autostart from
silently continuing to launch a stale v0.1.1 executable in its original folder.

## Verification completed

- Android Debug APK: compile, unit tests and `assembleDebug` passed (versionName
  `0.2.3`, versionCode `2004`). APK Signature Scheme v2 verified; signing
  certificate matches v0.1.1–v0.2.2 (`34cb31a3…2c7629`).
- Windows Agent: Release build and self-contained win-x64 publish passed. ZIP
  extraction confirmed the installer is included (474 files). Safe console tests
  passed including install layout/startup command generation.
- `Install-Or-Update.ps1 -WhatIf` passed: it printed target version/path and the
  intended Bentley-only process/file/Run-value operations without changing the
  actual PC installation, processes or registry.
- Protocol validation and Git whitespace checks passed. No real power action or
  real installation was run.

## Manual checks still required

Close any old tray copy, extract this ZIP, then run `Install-Or-Update.ps1` without
administrator rights. Confirm About shows the managed path, pairing is retained,
only one agent runs, and Windows reboot starts this v0.2.3 agent. Follow section
1a and scenario D in `TESTING.md`.

## SHA-256

- `BentleyRemote-v0.2.3-debug.apk`: `E2D530350963C462427117EEEB1D6DFF27B718A1D5DB2F6FA3123D2D770D9759`
- `BentleyRemote-v0.2.3-windows-x64.zip`: `8AB76CE14237861C73330A6D28D615F5AC54B762CD5E2F91B531324D6CE1ADE5`
