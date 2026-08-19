# Bentley Remote v0.2.6 — installer-only patch

- Исправлен ярлык меню Пуск: вместо зависящего от group-shell-resolution
  `{group}` Setup всегда создаёт доступный всем пользователям ярлык
  **Пуск → Bentley Remote → Bentley Remote Agent** через `{commonprograms}`.
- Выбор каталога, ярлык рабочего стола, автозапуск на фактический `{app}` и
  pairing/config сохранены без изменений.
- Android не менялся и APK намеренно не пересобирался/не дублировался.

## Проверки

- Windows Release build и safe console tests прошли.
- Inno Setup 6.7.3 скомпилировал Setup EXE; static checks подтверждают
  unconditional common Start-menu shortcut без task/condition.
- Ручной тест: после установки открыть **Пуск** и найти
  **Bentley Remote → Bentley Remote Agent**. Установщик на этом ПК не запускался.

## SHA-256

- `BentleyRemote-Setup-v0.2.6.exe`:
  `35014C4B6BE3761DFD6C6DF56E7F851F385F8B9AD3FDC8FD7DC5C81A145A67F0`
