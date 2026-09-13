# NimbMote roadmap

## Current baseline

NimbMote v0.10.1 is the current public test release. It provides local pairing,
Windows media and volume control, power actions, Android MediaSession controls,
and a Quick Settings Play/Pause tile. The public repository, download site, and
OpenAI Showcase submission materials use the NimbMote / Pink Electric identity.

## 1. Security gate before biometric Windows control

This phase is required before adding biometric authorization, Windows sign-in,
or any other feature that increases the consequences of a stolen phone or a
hostile local network.

- Encrypt and authenticate every post-pairing payload, not only the pairing
  secret and wallpaper preview. Derive a fresh session key per connection and
  use AEAD with unique nonces or sequence numbers.
- Reject replayed, reordered, tampered, and downgraded sessions. Never silently
  fall back from the encrypted protocol to the current plaintext channel.
- Strengthen first pairing with a high-entropy QR option or a PAKE-style
  exchange while retaining the six-digit code as an accessible fallback with a
  short lifetime and attempt limits.
- Add key rotation and explicit revocation. **Forget phone** on Windows and
  **Forget computer** on Android must invalidate the old relationship
  immediately.
- Restrict listeners and firewall rules to the intended private LAN/hotspot
  path; document that NimbMote must not be exposed through router port
  forwarding.
- Test passive capture, active man-in-the-middle modification, replay,
  downgrade, clock skew, app reinstall, Windows reinstall, and lost-phone
  recovery.

Acceptance criteria:

- A packet capture contains no readable media metadata, artwork, volume state,
  wallpaper data, or commands.
- Modified, duplicated, stale, or out-of-order messages are rejected.
- Revoking a device prevents it from reconnecting with its previous keys.
- Protocol migration is explicit and covered by Android, Windows, and
  cross-version tests.

## 2. Biometric authorization and Windows lock/sign-in

- Use Android `BiometricPrompt` and a hardware-backed Android Keystore key to
  prove recent user presence for privileged actions.
- Use a fresh Windows challenge, one-time nonce, and short expiry for every
  authorization. Windows must verify a cryptographic signature rather than
  trusting a boolean such as `fingerprintPassed` from the phone.
- Never store or transmit the user's Windows password. Research a Windows
  Credential Provider or another supported Windows authentication boundary for
  any future sign-in/unlock feature.
- Provide an immediate revoke action from the Windows tray for a lost or
  replaced phone.
- Decide separately which actions require biometrics. Locking the computer is
  low-risk; unlocking/sign-in and changing security state are high-risk and
  must always require fresh user presence.

Acceptance criteria:

- A copied request, stolen pairing database, or unlocked network session cannot
  authorize Windows sign-in.
- Cancelling or failing biometrics sends no privileged command.
- Revocation on the PC blocks the phone without requiring access to the phone.

## Later product work

- Phone microphone to Windows audio input.
- Windows audio streamed to the phone with seamless output switching.
- Optional Quick Settings power tile for lock, shutdown, and restart.
- Dedicated Android package-ID migration before Google Play distribution.

