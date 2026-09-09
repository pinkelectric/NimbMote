# NimbMote — Project State

## Current release

- Stable public-test release: **v0.9.0**.
- Public product name: **NimbMote**.
- Publisher name for public materials: **Pink Electric**.
- Android and Windows binaries are built and packaged for v0.9.0; checksums and verification notes are in `releases/v0.9.0/CHANGELOG.md` and `BUILD_STATUS.md`.

## What is working

- Android remote controls the active Windows media session, playback timeline, Windows volume and mute.
- The app shows the active Windows wallpaper — not a live desktop screenshot — plus shutdown and restart controls.
- First Windows install opens a six-digit pairing dialog; it closes after the phone pairs. The agent otherwise runs from the Windows system tray.
- Android exposes an optional Quick Settings Play/Pause tile so basic control can survive Android hiding the standard media card.
- Pairing works only across the same local network and is preserved across normal updates. Uninstall offers an explicit full-data-removal option.

## Deliberate compatibility boundary

The Android application ID, Windows executable, configuration folder, encrypted wire protocol, and some source namespaces retain legacy `BentleyRemote` / `Deskora` identifiers. They are not user-facing in v0.9.0 and must not be mechanically renamed: doing so can turn an update into a new installation and break existing pairings.

Before a Google Play release, make one dedicated migration decision for the Android package ID. That release must clearly state whether it is a clean installation and whether users need to pair again.

## Public-release cleanup in progress

1. Public repository renamed to `pinkelectric/NimbMote`; all new download links use the new repository. GitHub retains the old Deskora URL as a redirect.
2. Static hosting build is ready for the marketing/download site, including a standalone privacy page. The public-site base URL is configurable through `NIMBMOTE_PUBLIC_SITE_URL` at build time.
3. Create the Timeweb Cloud frontend in Almaty (Kazakhstan) from `main` with `npm run build:static` and `/dist/client`, then set `NIMBMOTE_PUBLIC_SITE_URL` to its technical URL. Keep Cloudflare as a development or backup deployment only.
4. After the Timeweb URL is live, validate the public site, metadata preview, downloads and privacy link, then prepare English Showcase submission material: hosted URL, public repository, short setup instructions, cover image, and an accurate statement that Codex was used to build the project.

## Known risks and next real-device checks

- On some Android devices the system can still remove an idle media session after several minutes. The Quick Settings tile is the intended fallback, but needs broader hardware confirmation.
- Samsung / One UI battery optimization can stop the client or its reconnect behavior. The in-app guidance points users to Unrestricted battery use.
- Test an Android reconnection after the Windows agent exits and restarts, after a Windows reboot, and after five to ten minutes of paused media.
- The Android APK is still a debug test package. A signed release package and a stable application ID decision are required before Google Play.

## Deferred scope

- Multiple phones or tablets paired with one PC.
- General support for arbitrary desktop-media applications beyond the Windows system media session.
- Microphone relay; the user currently uses AudioRelay for this separate need.
