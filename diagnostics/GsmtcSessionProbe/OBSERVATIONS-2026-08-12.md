# GSMTC / Microsoft Edge observations — 2026-08-12

## Среда

- Windows build: `10.0.26200.8973`.
- Microsoft Edge: `151.0.4129.72`.
- API: `GlobalSystemMediaTransportControlsSessionManager.GetSessions()`.
- Probe запускался в интерактивной пользовательской Windows-сессии.
- Стабильный Bentley Remote `v0.1.1` и его release-артефакты не изменялись.

## Наблюдаемый результат

Edge публиковал одну сессию с `SourceAppUserModelId=MSEdge`.

В коротком наблюдении получено 15 снимков. Одна и та же сессия (`ProbeId=1`,
неизменный runtime reference внутри процесса) последовательно содержала:

1. `synthwave radio 🌌 beats to chill/game to` / `Lofi Girl`, `Playing`;
2. другое YouTube-видео / `SpecterChannel`, сначала `Playing`, затем `Paused`.

При смене title, artist, timeline и набора controls:

- `GetSessions().Count` оставался равен `1`;
- `SessionsChanged` не возникал;
- `ProbeId` и runtime reference объекта из `GetSessions()` не менялись;
- возникали `MediaPropertiesChanged`, `PlaybackInfoChanged` и
  `TimelinePropertiesChanged`.

В отдельном 10-минутном наблюдении получено 107 снимков:

- во всех снимках `GetSessions().Count == 1`;
- `APPEARED=1`, `DISAPPEARED=0`;
- `SessionsChanged=0`, `CurrentSessionChanged=0`;
- paused-сессия сохранялась более шести минут, после возобновления продолжил
  использоваться тот же объект;
- отдельные paused-сессии для других Edge-медиавкладок не появились.

## Controls

Для единственной Edge-сессии API публиковал Play/Pause/Toggle и Seek. Наличие
Previous/Next менялось вместе с текущим медиаконтентом: для одного YouTube media
они были доступны, для другого — нет.

Адресная команда выбранной Edge-сессии не отправлялась, потому что условие теста
«`GetSessions()` вернул несколько Edge-сессий» не выполнилось. При единственной
сессии такая команда проверила бы только уже известное управление текущим
агрегированным Edge media target и не доказала бы независимое управление вкладками.

## Идентификация

Публичное свойство `SourceAppUserModelId` идентифицирует приложение (`MSEdge`),
но не вкладку. Публичного session ID или tab ID у
`GlobalSystemMediaTransportControlsSession` нет.

Объект, возвращаемый `GetCurrentSession()`, в этом процессе имел другой CLR/WinRT
wrapper, чем элемент `GetSessions()`: `ReferenceEquals`, `Equals` и object hash не
совпали; совпал только `SourceAppUserModelId`. Поэтому runtime reference полезен
для наблюдения за одним результатом `GetSessions()` внутри одного процесса probe,
но не является сохраняемым идентификатором вкладки или сессии между запусками.

## Вывод

В наблюдаемой версии Edge несколько медиавкладок мультиплексируются в одну GSMTC
session. Стандартный Windows Media Control API позволяет управлять этой сессией,
но не предоставляет список вкладок и не позволяет адресовать одну вкладку отдельно
от остальных. Для полноценного списка и независимого управления вкладками нужен
Edge Extension или другой browser-level канал, который имеет доступ к tab identity
и media elements.

Сырые локальные логи сохранены вне репозитория в `work/diagnostics/` и не входят в
release или Git.
