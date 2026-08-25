# Bentley Remote Protocol v1

Транспорт MVP — JSON-сообщения поверх одного постоянного WebSocket. Основной
порт Android-сервера: `45892`, путь `/bentley`. Резервный Windows-сервер слушает
`45893`, путь `/bentley/`.

UDP discovery использует порт `45894`. Для сохранённой пары probe/response
подписываются общим pairing secret; адрес Android всегда берётся из source
endpoint подписанного ответа. Для первичного сопряжения UDP используется только
в явном 10-минутном окне и доказывает знание шестизначного кода. Новый pairing
secret передаётся внутри AES-256-GCM, ключ выводится через ephemeral ECDH P-256 и
HKDF-SHA256; код используется для transcript proof и вывода ключа, а
идентификаторы входят в аутентифицированный transcript.

Bootstrap не считается PAKE: шестизначный код имеет ограниченную энтропию, а
после аутентификации основной WebSocket остаётся `ws://`. Поэтому v0.2.0
предназначена для доверенной домашней LAN/hotspot, а не для публичного Wi-Fi.
Подробный threat review и остаточные риски описаны в
`docs/SECURITY-v0.2.0.md`.

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
| `pair.reject` | Android → Windows | `reason` |
| `pair.request.v2` | Windows → Android | `clientId`, ephemeral `publicKey`, timestamp/nonce, code proof |
| `pair.accept.v2` | Android → Windows | идентификаторы, ephemeral `publicKey`, AES-GCM ciphertext и transcript proof |
| `auth.hello` | инициатор → принимающая сторона | `clientId`, `timestamp`, `nonce`, `proof` |
| `auth.ok` | принимающая сторона → инициатор | `serverId`, `serverName`, исходные `timestamp`/`nonce`, `proof` |
| `auth.error` | принимающая сторона → инициатор | `reason` |
| `state.snapshot` | Windows → Android | `media`, `volume` |
| `state.media` | Windows → Android | объект `MediaState` |
| `state.volume` | Windows → Android | объект `VolumeState` |
| `command.media` | Android → Windows | `action`, опционально `positionMs`/`offsetMs` |
| `command.volume` | Android → Windows | `action`, опционально `level`/`delta` |
| `system.action` | Android → Windows | только allowlist: `lock`, `sleep`, `restart`, `shutdown` |
| `command.result` | Windows → Android | `ok`, опционально `error` |
| `heartbeat.ping` | оба направления | `nonce` |
| `heartbeat.pong` | оба направления | `nonce` |
| `desktop.preview.request` | Android → Windows | случайный `requestId`; принимается только после authenticated pairing |
| `desktop.preview` | Windows → Android | `requestId`, `capturedAt`, `mimeType`, AES-GCM `nonce` и `ciphertext` |

`MediaState` содержит `hasSession`, `sessionId`, `sourceAppId`, `title`,
`artist`, `playbackStatus` (`playing`, `paused`, `stopped`, `closed`,
`changing`, `unknown`), `positionMs`, `durationMs`, capability-флаги и
опциональные `artworkMime`/`artworkBase64`. Размер artwork ограничен 512 KiB.

`VolumeState`: `level` в диапазоне `0.0..1.0` и `muted`.

Media actions: `play`, `pause`, `toggle`, `previous`, `next`, `seek`,
`seekBy`. Volume actions: `set`, `change`, `mute`, `unmute`, `toggleMute`.

## Desktop preview v0.3

При открытии главного экрана Android запрашивает один текущий preview; пользователь
может нажать Refresh. Нет polling, streaming, background capture или хранения
скриншотов: предыдущая картинка живёт только в памяти до следующего запроса.
Windows захватывает только primary interactive display, уменьшает JPEG до 1280×720
и отклоняет результат больше 1 MiB.

Обычная аутентификация WebSocket обязательна, но HMAC сам по себе не скрывает
данные. Поэтому image transfer отдельно шифруется AES-256-GCM. Ключ выводится
HKDF-SHA256 из pairing secret с domain separation
`bentley-remote/v1/desktop-preview/aes-256-gcm`; AAD включает requestId,
capturedAt и MIME type. На Android nonce одноразовый в пяти-минутнем окне,
размер ciphertext и декодированного JPEG ограничены до decrypt/decode.

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
