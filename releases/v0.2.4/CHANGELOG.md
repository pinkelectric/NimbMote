# Bentley Remote v0.2.4

## Что изменилось

- Windows-агент теперь поставляется как настоящий интерактивный
  `BentleyRemote-Setup-v0.2.4.exe`, собранный Inno Setup 6.7.3. Пользователь
  запускает Setup EXE двойным щелчком, подтверждает UAC и выбирает Install/Update.
- Установка и обновление выполняются в `C:\Program Files\Bentley Remote\`.
  Установщик останавливает только известный `BentleyRemote.Agent.exe`, сохраняет
  DPAPI pairing/config в `%LOCALAPPDATA%\BentleyRemote`, запускает новую версию
  и поддерживает одну запись `Bentley Remote` в HKLM Run.
- Установщик удаляет только известные устаревшие Bentley Remote значения HKCU
  Run (`Bentley Remote` и `BentleyRemote.Agent`); другие приложения и их
  автозапуск не затрагиваются.
- Tray → About / diagnostics… показывает версию и фактический путь EXE.
- APK сохраняет тот же debug signing certificate, поэтому его можно установить
  поверх предыдущей тестовой версии без сброса Android pairing state.

## Проверено автоматически

- Android offline: `compileDebugKotlin`, `testDebugUnitTest`, `assembleDebug`.
- Windows: Release build, self-contained `win-x64` publish и console tests
  (discovery/pairing cryptography, fake power actions, Program Files/startup
  layout intent).
- Настоящий Setup EXE скомпилирован Inno Setup 6.7.3; проверены product version,
  размер и статические правила скрипта. Установка на этом ПК намеренно не
  запускалась.

## Требует ручной проверки

1. Откройте Setup EXE двойным щелчком, подтвердите UAC и нажмите Install/Update.
2. В tray → About / diagnostics… проверьте версию `0.2.4` и путь
   `C:\Program Files\Bentley Remote\BentleyRemote.Agent.exe`.
3. Перезагрузите Windows: должна стартовать эта же версия, а сопряжение не должно
   потребовать повторной настройки.
4. Выполните сетевые и Galaxy A56/One UI сценарии из `TESTING.md`.

## SHA-256

- `BentleyRemote-v0.2.4-debug.apk`:
  `5889981EE961F9AAC555B8A40986E8EF7EEC1D6EB5A985A9865D172552EC3DEA`
- `BentleyRemote-Setup-v0.2.4.exe`:
  `5B1D8086DD78072ADF6F1CCC6C52B29F91B8EF49C83158676C2E4D17CB325C8D`
