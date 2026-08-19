# Bentley Remote v0.2.5

## Исправления

- Edge artwork теперь привязан к порядку `MediaPropertiesChanged`, а не только к
  title/artist. Новая ревизия немедленно очищает старую обложку; поздний результат
  предыдущего чтения отбрасывается. Диагностика показывает ревизию, причину,
  размер decoded artwork и stale discard без URL/приватных данных.
- Прогресс на главном экране Android плавно движется локально от последнего
  Windows timeline snapshot во время Playing, не опрашивая сеть. Pause, seek и
  свежий snapshot сразу rebasing позицию; значение ограничено duration.
- Setup EXE показывает страницу выбора каталога, всегда создаёт Start-menu
  shortcut и предлагает включённую задачу desktop shortcut. Автозапуск остаётся
  единственной записью Bentley Remote на фактический выбранный `{app}`; update
  сохраняет выбранный каталог и pairing/config.

## Проверки

- Android unit tests + debug APK build; Windows Release build + safe tests.
- Inno Setup 6.7.3 native package compilation and static installer checks.
- Реальные Edge и installer UI сценарии ещё требуют ручной проверки по
  `TESTING.md`; установка на этом ПК не запускалась.

## SHA-256

- `BentleyRemote-v0.2.5-debug.apk`:
  `27DFA2FF821E7446E7325F167EAD9891AA46C39A57B8B264F0C73AD7FCA48AEC`
- `BentleyRemote-Setup-v0.2.5.exe`:
  `BBE0A000A1F1FAE626071EA5DA097F73C3EA03C4AB5A0D2DE9C66AAF4E0F85D1`
