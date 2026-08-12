# Bentley Remote v0.2.0 — Galaxy A56 / One UI 8.5 test plan

Этот документ разделяет проверки, которые можно автоматизировать на любой
машине, и обязательные аппаратные проверки на реальном Samsung Galaxy A56.

## 1. До телефона

- [ ] `powershell -File .\scripts\validate-protocol.ps1` завершается без ошибок.
- [ ] `dotnet build .\windows\BentleyRemote.sln -c Release` проходит на Windows
  11 с .NET 8 SDK.
- [ ] Console tests подтверждают allowlist power actions, fake-controller,
  HMAC discovery, replay rejection, ECDH/AES-GCM pairing roundtrip и loopback
  UDP responder; тесты не выполняют реальные системные действия.
- [ ] `android\gradlew.bat :app:testDebugUnitTest --offline --no-daemon`
  подтверждает шаг громкости 1% и состав power UI.
- [ ] `android\gradlew.bat :app:assembleDebug` проходит с JDK 17 и SDK 35.
- [ ] В APK manifest присутствуют INTERNET, POST_NOTIFICATIONS,
  FOREGROUND_SERVICE и FOREGROUND_SERVICE_MEDIA_PLAYBACK.
- [ ] `protocol/examples/state.snapshot.json` читается обеими реализациями.

## 2. Установка и базовый UI — реальный A56

- [ ] Установить debug APK вручную/ADB; приложение запускается без crash.
- [ ] Разрешить notifications; проверить, что отказ не ломает основной экран.
- [ ] Страница показывает Disconnected, поле шестизначного кода Windows и
  неактивные media/power controls.
- [ ] Выбрать Battery → Unrestricted и убрать приложение из Deep sleeping apps.

## 3. Сетевые сценарии v0.2.0 — обязательно на реальном оборудовании

### A. Upgrade существующей пары v0.1.1

- [ ] Не удаляя v0.1.1, установить v0.2.0 APK поверх неё; заменить Windows-агент,
  предварительно закрыв старый экземпляр из tray.
- [ ] Старые Android Keystore/SharedPreferences и Windows DPAPI config подхвачены;
  повторный pairing не требуется.
- [ ] В общей LAN соединение сначала пробует last-known address, затем gateway и
  authenticated LAN discovery; после DHCP-смены адреса соединение восстанавливается.

### B. Clean install в общей Wi-Fi/LAN или hotspot стороннего телефона

- [ ] Удалить/сбросить pairing на обеих сторонах; Windows и Galaxy подключить к
  одной сети, где gateway не является Galaxy.
- [ ] В tray Windows открыть `Pair with phone…`, получить временный 6-значный код.
- [ ] Ввести код на Android и нажать поиск; IP вручную не вводить.
- [ ] Bootstrap discovery находит именно окно с этим кодом, pairing завершается,
  а последующие reconnect используют только signed paired discovery.
- [ ] Неверный/просроченный код, повтор nonce и неподписанный UDP response отклоняются.

### C. Регрессия Galaxy hotspot

> **Нельзя достоверно проверить без A56:** входящий TCP/WebSocket на интерфейсе
> мобильной точки доступа. Эмулятор и обычный роутер этого не воспроизводят.

- [ ] Включить hotspot на A56, подключить Windows 11 к нему.
- [ ] Сверить, что gateway Windows — адрес телефона, но не записывать/хардкодить
  этот адрес в конфигурацию агента.
- [ ] Открыть приложение; `Test-NetConnection <gateway> -Port 45892` возвращает
  `TcpTestSucceeded: True`.
- [ ] Уже сопряжённая пара подключается быстрым gateway-путём без UDP-задержки.
- [ ] Перезапустить hotspot и убедиться, что после смены IP агент сам находит
  новый gateway и восстанавливает канал не позднее 15 секунд.
- [ ] На 10 секунд выключить Wi-Fi компьютера, включить и проверить reconnect.
- [ ] Выключить мобильные данные при сохранённом локальном Wi-Fi: управление
  продолжает работать.

## 4. Pairing и security

- [ ] Неверный код получает `pair.reject`, команды не принимаются.
- [ ] Верный код даёт Connected на обеих сторонах.
- [ ] Закрыть/открыть агент и приложение: соединение восстанавливается без кода.
- [ ] Изменить timestamp auth более чем на 120 секунд: auth отклоняется.
- [ ] Повторить одинаковый nonce: второй запрос отклоняется.
- [ ] С другого клиента без secret отправить `command.volume`: сокет закрывается.
- [ ] «Forget computer» инвалидирует старый Windows secret.
- [ ] На Windows проверить, что JSON не содержит secret в открытом виде; после
  копирования файла в другой Windows account DPAPI не расшифровывает его.

## 5. Edge / YouTube vertical slice

- [ ] В Edge запустить обычное YouTube-видео; title появляется в Android UI.
- [ ] Отображаются playing/paused и актуальный progress/duration.
- [ ] Если Windows предоставляет thumbnail, он виден в UI и медиакарточке.
- [ ] Play и Pause из приложения управляют Edge.
- [ ] Play/Pause из системной карточки управляют Edge.
- [ ] Previous/Next включены только если GSMTC объявляет capability, и работают.
- [ ] ±10 секунд и drag seek меняют позицию; live stream без seek не ломается.
- [ ] После переключения Edge на другой ролик обновляются title, duration и art.
- [ ] После закрытия Edge показывается отсутствие активной сессии.
- [ ] Запустить другое GSMTC-приложение (например Spotify/VLC при наличии) и
  проверить, что Windows current session переключается без рестарта агента.

## 6. Windows volume

- [ ] Slider меняет master volume Windows с погрешностью не более 1%.
- [ ] Mute/Unmute синхронизируется в обе стороны в течение 2 секунд.
- [ ] Изменить Windows volume мышью/клавиатурой: Android slider обновляется.
- [ ] Переключить default output (динамики → Bluetooth/HDMI): после паузы до 2
  секунд агент начинает читать/менять новый endpoint.
- [ ] Физическая Volume Up/Down через remote MediaSession изменяет Windows ровно
  на 1 процентный пункт; шкала остаётся 0..100 и синхронизируется обратно.

## 6a. Ручные системные действия Windows — только пользовательский тест

> Автоматические тесты используют fake-controller и никогда не блокируют, не
> усыпляют, не перезагружают и не выключают реальный компьютер.

- [ ] Без authenticated connection обе основные кнопки и overflow disabled.
- [ ] На экране постоянно видны только «Выключить» и «Перезагрузить»; «Сон» и
  «Заблокировать» находятся только в меню `⋮`.
- [ ] Shutdown, Restart и Sleep требуют отдельного понятного подтверждения;
  Lock выполняется сразу после выбора из overflow.
- [ ] Проверить Lock, затем Sleep, Restart и Shutdown по одному, сохранив работу
  перед тестом; Android показывает accepted либо понятную ошибку.

## 7. One UI media UX — обязательно реальный A56

> **Нельзя достоверно проверить без A56 / One UI 8.5:** оформление Samsung
> media card, lock-screen placement и маршрутизация физических volume keys.

- [ ] При активной Windows-сессии появляется медиакарточка в quick settings.
- [ ] На lock screen карточка видна согласно настройкам уведомлений Samsung.
- [ ] Карточка показывает правильные title/art/state/progress.
- [ ] Кнопки карточки не запускают локальный звук и не создают audio focus.
- [ ] Нажать физические Volume Up/Down при активной карточке: отметить, меняется
  ли Windows master volume или локальный media volume телефона.
- [ ] Если One UI показывает remote output volume panel, проверить диапазон
  0..100, шаг и mute.
- [ ] Повторить тест физических кнопок при погашенном экране и на lock screen.
- [ ] Зафиксировать фактическое поведение: публичный Media3 remote volume API не
  гарантирует, что OEM направит аппаратные клавиши именно этой session.

## 8. Background / Samsung power management — обязательно реальный A56

> **Нельзя достоверно проверить эмулятором:** убийство процесса One UI,
> foreground media policy и deep-sleep heuristics.

- [ ] Смахнуть Activity из recent apps во время playback: карточка и команды
  остаются рабочими минимум 30 минут.
- [ ] Поставить Edge на pause, погасить экран на 30 минут, затем возобновить с
  карточки. Отметить, сохранился ли сервис.
- [ ] Повторить с Battery Optimized и Unrestricted, записать различие.
- [ ] Перезапустить Android-процесс/телефон: приложение требует ручного запуска
  (boot receiver в MVP отсутствует), после запуска secret сохраняется.
- [ ] Отключить notification permission и проверить деградацию; вернуть его.

## 9. Fallback Android → Windows

- [ ] Выполнить admin-скрипт URL ACL/firewall и проверить listener TCP 45893.
- [ ] Заблокировать/не запускать Android server path, включить fallback и ввести
  IPv4 Windows на hotspot-интерфейсе.
- [ ] Android инициирует auth и получает state/commands по обратному каналу.
- [ ] Удалить firewall rule: соединение не устанавливается, UI не зависает,
  backoff ограничен 10 секундами.
- [ ] Вернуть правило и проверить автоматический reconnect.

## 10. Результаты, которые нужно записать

Для каждого провала приложить: версия One UI/build number, Android version,
тип hotspot security, gateway/PC IPv4, вывод `Test-NetConnection`, действие,
ожидаемый и фактический результат, logcat строки `com.bentley.remote`, а для
Windows — текст статуса tray и Event Viewer/.NET exception при наличии.
