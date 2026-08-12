# Bentley Remote v0.2.0

Дата сборки: 2026-08-12

Статус: отдельная тестовая версия. Автоматические сборки и безопасные тесты
пройдены; проверка на реальном Galaxy A56 в сторонней Wi-Fi/LAN/hotspot и ручная
проверка системных действий Windows ещё предстоят.

## Исправлено

- Устранена корневая причина подключения только к hotspot самого Galaxy:
  Windows-агент больше не считает default gateway единственным адресом телефона.
- Для существующей пары добавлен каскад reconnect: активный socket, last-known
  address, прежний быстрый gateway-путь, authenticated UDP LAN discovery и
  существующий manual reverse fallback.
- Paired discovery подписывает probe/response сохранённым HMAC secret, проверяет
  identity, timestamp, nonce и endpoint; адрес Android берётся из UDP source.
- Добавлены single-winner active socket, heartbeat/stale timeout, bounded
  exponential backoff с jitter, освобождение UDP/WebSocket при остановке и
  сохранение последнего успешного IPv4.
- Относительный шаг физических кнопок remote volume изменён с 5% на 1%; шкала
  0..100, абсолютная установка и обратная синхронизация Windows сохранены.

## Первичное сопряжение без IP

- Windows tray открывает явное 10-минутное окно и показывает временный
  шестизначный код; Android вводит код и ищет Windows broadcast в общей LAN.
- Bootstrap UDP только проверяет код и сообщает source endpoint. Новый secret не
  передаётся в UDP и создаётся лишь после полного pairing flow.
- Выдача secret защищена ephemeral ECDH P-256, HKDF-SHA256 и AES-256-GCM с
  transcript proof. Replay/чужая identity/неверный HMAC отклоняются.
- Форматы Android Keystore/SharedPreferences, Windows DPAPI и debug signing key
  v0.1.1 сохранены, поэтому установка поверх v0.1.1 не должна требовать pairing.
- Ограничение честно задокументировано: шестизначный bootstrap не является PAKE,
  а основной канал остаётся `ws://`; использовать только в доверенной домашней
  LAN или личном hotspot, не в публичной Wi-Fi.

## Управление компьютером

- Добавлена строго типизированная allowlist-команда `system.action`: только
  `lock`, `sleep`, `restart`, `shutdown`, без shell-строк, путей и аргументов.
- Windows использует отдельный dispatcher/controller: LockWorkStation,
  SetSuspendState и `shutdown.exe` только с фиксированными аргументами.
- На основном экране постоянно видны только «Выключить» и «Перезагрузить»;
  «Сон» и «Заблокировать» находятся только в меню `⋮`.
- Shutdown, Restart и Sleep требуют подтверждения; Lock — нет. Все пункты
  недоступны без authenticated connection и показывают accepted/failed status.

## Проверки

- Android `compileDebugKotlin`, `testDebugUnitTest`, `assembleDebug`: успешно;
  unit-тесты подтверждают шаг 1% и точный состав primary/overflow power actions.
- APK: `versionName=0.2.0`, `versionCode=2001`, target SDK 35; подпись v2 успешна,
  certificate SHA-256 совпадает с v0.1.1:
  `34cb31a3fb393e034948eba1ba007215ea8025a4759fc8bf4dff2e3cbd2c7629`.
- Manifest: mediaPlayback foreground service и прежние notification/foreground
  permissions сохранены; для UDP discovery не добавлены location/SSID permissions.
- Windows Release build и self-contained win-x64 publish: успешно, 0 warnings,
  File/Product version `0.2.0.0` / `0.2.0`.
- Безопасные Windows-тесты: power allowlist/fake mapping, paired HMAC discovery,
  bootstrap proof/replay, ECDH/AES-GCM roundtrip, directed broadcast и loopback
  signed UDP responder — успешно. Реальные power actions тестами не выполнялись.
- Protocol examples/HMAC vector: успешно. В Windows ZIP нет временной утилиты
  `GsmtcSessionProbe` и нет test harness.

## Что проверить вручную

- Сценарий A: установить APK поверх v0.1.1 и подтвердить сохранение пары.
- Сценарий B: clean install, общая сеть через роутер/сторонний hotspot, pairing по
  коду без IP и reconnect после DHCP-смены.
- Сценарий C: регрессия прежнего Galaxy-hotspot gateway-пути.
- Физические кнопки громкости — ровно 1/100.
- По одному проверить Lock, Sleep, Restart и Shutdown после сохранения работы.

Полный порядок приведён в `TESTING.md` исходного репозитория.

## Артефакты

- `BentleyRemote-v0.2.0-debug.apk`
  - SHA-256: `a321ff1bc14996d5d6e3ce755895798bf61032c62762325caeff23eeeb212215`
- `BentleyRemote-v0.2.0-windows-x64.zip`
  - SHA-256: `76cec81ae9d4abacaf07ecd2b9b162b7cdaa52feb429e336205060d752a4c84f`
