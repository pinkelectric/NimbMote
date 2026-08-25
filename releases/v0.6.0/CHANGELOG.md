# Bentley Remote v0.6.0

- Added a visible reload button on the media card. It sends F5 only to the foreground Microsoft Edge tab.
- Added **Restore YouTube** to the computer menu. It reloads a foreground YouTube tab and then asks Windows to resume playback.
- Browser control is an authenticated, two-action allowlist; it cannot open URLs or send arbitrary keyboard input.

## Verification

- Android debug unit tests and APK assembly: passed.
- Windows agent tests and self-contained x64 publish: passed.
- Protocol validation: passed.
- APK signature verification: v2, one signer.

## SHA-256

- `BentleyRemote-v0.6.0-debug.apk`: `96C1E7C9E1A37009BF17058EE0063C0BFD6C01D9A78C2D3A668E8840D13D162E`
- `BentleyRemote-Setup-v0.6.0.exe`: `E2588DAB878D44F21C33A7458944961F4563FA7CF7FBD8EC9E4F647F0B0E8647`
