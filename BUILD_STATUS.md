# Build status — v0.2.6 — 2026-08-19

- Android is unchanged and was intentionally not rebuilt or duplicated in this
  installer-only patch.
- Windows: Release build and safe console tests pass. The loopback diagnostic
  test now uses an ephemeral local port and does not collide with a running agent.
- Setup: native Inno Setup 6.7.3 package compiled from the self-contained x64
  publish. Static inspection confirms the directory page, Start-menu icon,
  checked-by-default desktop task, unconditional `{commonprograms}` Start-menu
  target, `{app}` Run target and narrow legacy cleanup.
- Hardware validation remains in `TESTING.md`.
