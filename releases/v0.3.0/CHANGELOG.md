# Bentley Remote v0.3.0

## Changed

- The Android screen starts with a paired-computer card: computer name, status,
  on-demand primary-desktop preview, refresh, shutdown, restart and overflow
  actions. Media and Windows volume follow it.
- The preview is requested once when Bentley opens or reconnects, and on manual
  refresh. It is not streamed, polled in the background or stored on Windows.
- The Windows agent captures only the current interactive primary display and
  limits the JPEG preview to 1280x720 and 1 MiB.
- Preview bytes are AES-256-GCM encrypted with a key derived from the existing
  pairing secret; replay, nonce and size checks protect the Android receiver.
- Android now supplies separate English and Russian UI strings from the device
  locale. Multiple phones/tablets per PC remain deferred to v0.4+.

## Verification

- Android `compileReleaseKotlin`, unit tests and release package passed.
- Windows Release/self-contained publish and Inno Setup package passed.
- APK signature verified with v2 and v3; Windows Setup was compiled but not run.

## SHA-256

- `BentleyRemote-v0.3.0-release.apk`:
  `5499741678DD154533135B4C97D0821A661A847D1B4BEF0F55CFD75EC9DED154`
- `BentleyRemote-Setup-v0.3.0.exe`:
  `C6101B97EEBE126B953EF04083CCF0E3D035C4978D1F61E5FC5D7FBEAFA85A56`
