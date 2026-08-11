# Bentley Remote Protocol v1

Транспорт MVP — JSON-сообщения поверх одного постоянного WebSocket. Основной
порт Android-сервера: `45892`, путь `/bentley`. Резервный Windows-сервер слушает
`45893`, путь `/bentley/`.

Каждое сообщение имеет envelope:

```json
{
  "version": 1,
  "type": "command.media",
  "id": "56e51f24-8aa0-4c07-a13d-74ec2fd24182",
  "replyTo": null,
  "sentAt": 1786392000000,
  "payload": { "action": "pause" }
}
```

- `version` — версия wire protocol; неизвестная major-версия отклоняется.
- `type` — тип сообщения из таблицы ниже.
- `id` — UUID сообщения.
- `replyTo` — UUID запроса для ответа, иначе `null`.
- `sentAt` — Unix time в миллисекундах.
- `payload` — объект конкретного сообщения.

## Сообщения

| Тип | Направление | Payload |
|---|---|---|
| `pair.request` | Windows → Android | `clientId`, `clientName`, `code` |
| `pair.accept` | Android → Windows | `serverId`, `serverName`, `clientId`, `secret` (base64) |
| `pair.reject` | Android → Windows | `reason` |
| `auth.hello` | инициатор → принимающая сторона | `clientId`, `timestamp`, `nonce`, `proof` |
| `auth.ok` | принимающая сторона → инициатор | `serverId`, `serverName`, исходные `timestamp`/`nonce`, `proof` |
| `auth.error` | принимающая сторона → инициатор | `reason` |
| `state.snapshot` | Windows → Android | `media`, `volume` |
| `state.media` | Windows → Android | объект `MediaState` |
| `state.volume` | Windows → Android | объект `VolumeState` |
| `command.media` | Android → Windows | `action`, опционально `positionMs`/`offsetMs` |
| `command.volume` | Android → Windows | `action`, опционально `level`/`delta` |
| `command.result` | Windows → Android | `ok`, опционально `error` |
| `heartbeat.ping` | оба направления | `nonce` |
| `heartbeat.pong` | оба направления | `nonce` |

`MediaState` содержит `hasSession`, `sessionId`, `sourceAppId`, `title`,
`artist`, `playbackStatus` (`playing`, `paused`, `stopped`, `closed`,
`changing`, `unknown`), `positionMs`, `durationMs`, capability-флаги и
опциональные `artworkMime`/`artworkBase64`. Размер artwork ограничен 512 KiB.

`VolumeState`: `level` в диапазоне `0.0..1.0` и `muted`.

Media actions: `play`, `pause`, `toggle`, `previous`, `next`, `seek`,
`seekBy`. Volume actions: `set`, `change`, `mute`, `unmute`, `toggleMute`.

## Аутентификация

После первичного сопряжения стороны хранят 32-байтовый общий секрет. Инициатор
создаёт `timestamp`, случайный base64 `nonce` и вычисляет:

```text
proof = base64(HMAC-SHA256(secret, clientId + "\n" + timestamp + "\n" + nonce))
```

Кросс-платформенный контрольный вектор находится в
`examples/hmac-test-vector.json` и проверяется `scripts/validate-protocol.ps1`.

Принимающая сторона проверяет идентификатор, HMAC, окно времени ±120 секунд и
повтор nonce. В `auth.ok` она возвращает тот же challenge и собственный HMAC,
вычисленный с `serverId` вместо `clientId`; инициатор также проверяет его. Так
аутентификация после pairing является взаимной. До успешной проверки
`auth.ok` любые команды и состояния игнорируются.

Важно: MVP использует `ws://`, а не TLS. Одноразовый код не защищает первичную
передачу секрета от активного/пассивного наблюдателя в той же сети. Первое
сопряжение выполняйте только на личной WPA2/WPA3 hotspot-сети. После сопряжения
HMAC не позволяет неизвестному устройству отправлять команды или подменять
состояние. Будущее расширение протокола может заменить первичную процедуру на
PAKE (например SPAKE2+) без изменения командного слоя.
