# Bentley Remote v0.1.0

Дата сборки: 2026-08-11

Статус: стабильная тестовая сборка для первой реальной установки.

## Что входит

- Android-приложение с Media3 remote media session, системной медиакарточкой,
  управлением воспроизведением, seek, общей громкостью Windows и mute.
- Самодостаточный Windows 11 x64 tray-агент с GSMTC, Core Audio, pairing,
  HMAC-аутентификацией, reconnect и резервным reverse-режимом.
- Локальная работа телефон ↔ ПК через hotspot без облака и аккаунтов.

## Проверки

- Android `assembleDebug`: успешно, 36 задач.
- APK Signature Scheme v2: успешно.
- Встроенная Android-версия: `versionName=0.1.0`, `versionCode=1001`.
- Windows `dotnet publish`: успешно, Release, win-x64, self-contained.
- Windows Assembly/File/Product version: `0.1.0.0` / `0.1.0.0` / `0.1.0`.
- JSON-примеры протокола и HMAC-SHA256 test vector: успешно.
- Аппаратная проверка на Galaxy A56 и Windows 11 выполняется после установки по
  корневому `TESTING.md`.

## Артефакты

- `BentleyRemote-v0.1.0-debug.apk`
  - SHA-256: `2b54b81446948272daeeaeb1cfe95674aeb5db61b9e463fa89f0d5a58708357a`
- `BentleyRemote-v0.1.0-windows-x64.zip`
  - SHA-256: `04af5478d03b05f3a61e3ad862fc6e30e07cfcfa57b67f53454cae82964e7199`
