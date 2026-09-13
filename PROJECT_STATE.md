# NimbMote — Project State

## Current release

- Latest local test release: **v0.10.1**.
- Public product name: **NimbMote**.
- Publisher name for public materials: **Pink Electric**.
- Android and Windows binaries are built and packaged for v0.10.1; checksums and verification notes are in `releases/v0.10.1/CHANGELOG.md` and `BUILD_STATUS.md`.

## Local build toolchains

- Use the repository's portable toolchains, not a system-wide installation: `../work/toolchains/jdk-17/jdk-17.0.20+8`, `../work/toolchains/android-sdk`, and `../work/toolchains/dotnet` relative to the `files-mentioned-by-the-user-bentley` workspace root.
- `scripts/build-all.ps1` detects those paths and exports `JAVA_HOME`, `ANDROID_HOME`, `GRADLE_USER_HOME`, and the portable .NET executable automatically. Do not install another JDK merely because a fresh shell lacks `java` on `PATH`.

## What is working

- Android remote controls the active Windows media session, playback timeline, Windows volume and mute.
- The app shows the active Windows wallpaper — not a live desktop screenshot — plus shutdown and restart controls.
- First Windows install opens a six-digit pairing dialog; it closes after the phone pairs. The agent otherwise runs from the Windows system tray.
- Android exposes an optional Quick Settings Play/Pause tile so basic control can survive Android hiding the standard media card.
- Pairing works only across the same local network and is preserved across normal updates. Uninstall offers an explicit full-data-removal option.

## Deliberate compatibility boundary

The Android application ID, Windows executable, configuration folder, encrypted wire protocol, and some source namespaces retain legacy `BentleyRemote` / `Deskora` identifiers. They are not user-facing in v0.9.0 and must not be mechanically renamed: doing so can turn an update into a new installation and break existing pairings.

Before a Google Play release, make one dedicated migration decision for the Android package ID. That release must clearly state whether it is a clean installation and whether users need to pair again.

## Public release and Showcase

1. The public repository is `pinkelectric/NimbMote`; v0.10.1 and its release tags are published there.
2. The marketing/download site and standalone privacy page are live on Cloudflare. The public-site base URL remains configurable through `NIMBMOTE_PUBLIC_SITE_URL`.
3. A non-Cloudflare mirror remains optional for easier access from Russia. Timeweb is deferred because its minimum account deposit is disproportionate to the current test traffic.
4. OpenAI Showcase materials are prepared in `docs/OPENAI_SHOWCASE_SUBMISSION.md`; the project fields are filled in the official form, while the submitter must add personal contact fields, accept the agreement, and submit it.

## Roadmap decision

The next security iteration is a release gate before biometric Windows control or sign-in. It must encrypt the complete local session, harden pairing and revocation, and add adversarial protocol tests. The ordered plan and acceptance criteria are in `docs/ROADMAP.md`.

## Known risks and next real-device checks

- On some Android devices the system can still remove an idle media session after several minutes. The Quick Settings tile is the intended fallback, but needs broader hardware confirmation.
- Samsung / One UI battery optimization can stop the client or its reconnect behavior. The in-app guidance points users to Unrestricted battery use.
- Test an Android reconnection after the Windows agent exits and restarts, after a Windows reboot, and after five to ten minutes of paused media.
- The Android APK is still a debug test package. A signed release package and a stable application ID decision are required before Google Play.

## Deferred scope

- Multiple phones or tablets paired with one PC.
- General support for arbitrary desktop-media applications beyond the Windows system media session.
- Microphone relay; the user currently uses AudioRelay for this separate need.
