# Galaxy A56 / One UI 8.5 test plan

Этот документ разделяет проверки, которые можно автоматизировать на любой
машине, и обязательные аппаратные проверки на реальном Samsung Galaxy A56.

## 1. До телефона

- [ ] `powershell -File .\scripts\validate-protocol.ps1` завершается без ошибок.
- [ ] `dotnet build .\windows\BentleyRemote.sln -c Release` проходит на Windows
  11 с .NET 8 SDK.
- [ ] `android\gradlew.bat :app:assembleDebug` проходит с JDK 17 и SDK 35.
- [ ] В APK manifest присутствуют INTERNET, POST_NOTIFICATIONS,
  FOREGROUND_SERVICE и FOREGROUND_SERVICE_MEDIA_PLAYBACK.
- [ ] `protocol/examples/state.snapshot.json` читается обеими реализациями.

## 2. Установка и базовый UI — реальный A56

- [ ] Установить debug APK вручную/ADB; приложение запускается без crash.
- [ ] Разрешить notifications; проверить, что отказ не ломает основной экран.
- [ ] Страница показывает Disconnected, шестизначный pairing code и inactive
  media controls.
- [ ] Перезапустить процесс до pairing: появляется новый валидный код.
- [ ] Выбрать Battery → Unrestricted и убрать приложение из Deep sleeping apps.

## 3. Основной hotspot path — обязательно реальный A56

> **Нельзя достоверно проверить без A56:** входящий TCP/WebSocket на интерфейсе
> мобильной точки доступа. Эмулятор и обычный роутер этого не воспроизводят.

- [ ] Включить hotspot на A56, подключить Windows 11 к нему.
- [ ] Сверить, что gateway Windows — адрес телефона, но не записывать/хардкодить
  этот адрес в конфигурацию агента.
- [ ] Открыть приложение; `Test-NetConnection <gateway> -Port 45892` возвращает
  `TcpTestSucceeded: True`.
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

