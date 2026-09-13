# OpenAI Showcase submission packet

Prepared for the [OpenAI Showcase submission form](https://openai.com/form/showcase-submission/).

## Public links

- GitHub repository: https://github.com/pinkelectric/NimbMote
- Hosted project page: https://nimbmote.pinkelectric.workers.dev
- Privacy page: https://nimbmote.pinkelectric.workers.dev/privacy
- Public cover image: https://nimbmote.pinkelectric.workers.dev/og-nimbmote.png
- Author display name: `Pink Electric`

## About the project

### What type of project are you submitting?

Open-source desktop companion app

### Did you use Codex to build this?

Yes

### Did you use another coding agent to build this?

No

This answer describes implementation. If code from another coding agent is added before submission, change it to `Yes`.

### What is the tech stack used in the project?

Kotlin, Jetpack Compose, Android Media3/MediaSession, WebSocket, C#/.NET 8, WinForms, Windows GSMTC and Core Audio, Next.js/React, and Cloudflare Workers.

### List use cases showcased in your project

Personal productivity; local-device control; accessibility; remote Windows media, volume, and power control from an Android phone.

### Which capability are you showcasing?

Codex-assisted end-to-end product development across two operating systems: turning user-led hardware testing into coordinated Android, Windows, networking, security, installer, automated-test, release, and documentation changes.

### Which OpenAI models and APIs are you using in your project?

OpenAI Codex was used as the coding agent throughout development. NimbMote itself does not call an OpenAI model or API at runtime.

### Are you using other models or APIs in your project?

No other AI models. Platform APIs include Android MediaSession and Quick Settings, Windows GSMTC and Core Audio, and local WebSocket networking.

### Building process

NimbMote began as a practical need for controlling a Windows PC from a phone. The author tested every iteration on real Android and Windows hardware, described failures and UX friction, and used Codex to inspect logs, implement fixes across both clients, add regression tests, build installers, prepare releases, and maintain the public site and documentation.

## Project details

### Setup steps

Install NimbMote Agent on a Windows 10/11 x64 PC and install the NimbMote test APK on Android 8 or newer. Put both devices on the same Wi-Fi or phone hotspot. Open the Agent to see its six-digit code, enter that code in the Android app, and tap Search and pair on this LAN. On Samsung/One UI, set battery use to Unrestricted for reliable background controls.

### Title

NimbMote — Control Windows from Android

### Tagline

A private local-network remote for Windows media, volume, and power controls—right from an Android phone.

### Project description

NimbMote pairs an Android phone with a Windows PC over the same local network, then puts the active Windows media session, master volume, wallpaper preview, and explicit sleep, lock, shutdown, and restart controls in one mobile interface. It also integrates with Android's system media card and Quick Settings for controls away from the main app.

The product needs no NimbMote account, browser extension, cloud relay, advertising, or analytics. Pairing uses a one-time six-digit code; saved pairing secrets are protected by Android Keystore and Windows DPAPI. NimbMote is currently available as an open-source public test build for Windows 10/11 and Android 8+.

### Author name

Pink Electric

## Submitter-supplied fields

The submitter must personally provide these fields and accept the agreement:

- First name
- Last name
- Email
- Personal website or social profile (optional)
- Agreement attestation

## Release used for submission

The public site and repository offer the hardware-tested v0.10.1 build. Its Android media-card repair and reconnect behavior were confirmed on a real phone before publication.
