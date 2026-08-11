# Bentley Remote v0.1.1

Дата сборки: 2026-08-11

Статус: стабильная тестовая сборка для проверки медиакарточки на Galaxy A56 / One UI 8.5.

## Исправлено

- Существующая `MediaSession` явно зарегистрирована в `MediaSessionService`, чтобы
  штатный notification manager Media3 начал наблюдать за её состоянием и публиковать
  `MediaNotification`.
- Добавлен настоящий Android notification channel `bentley_remote_media` с importance
  `LOW`; штатный `DefaultMediaNotificationProvider` Media3 закреплён за этим каналом.
- Добавлены временные диагностические записи `BentleyRemoteService` для проверки
  канала, активного уведомления, foreground-состояния сервиса и MediaSession.

## Без изменений

- Сетевое соединение, hotspot-режим, pairing, протокол, reconnect, Windows-агент,
  VolumeProvider/удалённая громкость, MediaSession-команды, метаданные и обложки.

## Проверки сборки

- Android `assembleDebug`: успешно, 36 задач.
- Встроенная Android-версия: `versionName=0.1.1`, `versionCode=1002`.
- Manifest: `MediaSessionService`, foreground type `mediaPlayback`, permissions
  `FOREGROUND_SERVICE` и `FOREGROUND_SERVICE_MEDIA_PLAYBACK` присутствуют.
- APK Signature Scheme v2: успешно; сертификат совпадает с `v0.1.0`.
- Windows `dotnet publish`: успешно, Release, win-x64, self-contained.
- Windows File/Product version: `0.1.1.0` / `0.1.1`.
- JSON-примеры протокола и HMAC-SHA256 test vector: успешно.

## Артефакты

- `BentleyRemote-v0.1.1-debug.apk`
  - SHA-256: `fc55b7ced2aa713c120d7f16c274a230e6342e9b25ecd97c15ec17b6b6d858d1`
- `BentleyRemote-v0.1.1-windows-x64.zip`
  - SHA-256: `7ddc1ed069f3516ef9765530c3741ccf11682e3b7216646a519c20514548da51`
