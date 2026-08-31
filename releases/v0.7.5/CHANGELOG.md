# Deskora v0.7.5 — Windows hotfix

## First-install pairing window

- Fixed the fresh-install detector in the Windows installer. It now records whether Deskora was installed **before** setup begins, instead of checking after setup has already created its own registry key.
- After a full uninstall that removes saved data, the next installation now opens the six-digit pairing-code window automatically.
- An ordinary update remains quiet and does not interrupt a paired user.

## Scope and verification

- Windows-only hotfix; Android app was not rebuilt or sent to the phone.
- Windows agent test harness: passed.
- Installer script: compiled successfully with Inno Setup 6.7.3.

## SHA-256

- `Deskora-Setup-v0.7.5.exe`:
  `3B49CA02DB084050C48A56387142C531DDD8CEA554FA53C340BF6E657D7A4E8B`
