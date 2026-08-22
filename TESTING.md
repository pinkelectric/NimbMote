# Bentley Remote v0.2.7 — Galaxy A56 / One UI 8.5 test plan

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
  FOREGROUND_SERVICE, FOREGROUND_SERVICE_MEDIA_PLAYBACK,
  FOREGROUND_SERVICE_CONNECTED_DEVICE, CHANGE_WIFI_STATE и RECEIVE_BOOT_COMPLETED.
- [ ] `protocol/examples/state.snapshot.json` читается обеими реализациями.
- [ ] Windows console tests подтверждают install layout: Program Files path,
  единственное известное имя Run-value и legacy-value cleanup intent.

## 1a. Установка/обновление Windows agent — пользовательский тест

- [ ] Открыть `BentleyRemote-Setup-v0.2.5.exe` двойным щелчком, подтвердить UAC
  и нажать **Install** / **Update**. Распаковка или PowerShell не требуются.
- [ ] После завершения в tray → About / diagnostics… версия соответствует
  v0.2.5, а Executable указывает на
  `C:\Program Files\Bentley Remote\BentleyRemote.Agent.exe`, не на старую папку
  v0.1.1/v0.2.x.
- [ ] В `HKLM\Software\Microsoft\Windows\CurrentVersion\Run` остаётся только
  известное значение `Bentley Remote`, указывающее на установленный EXE; старые
  известные Bentley значения в HKCU удалены.
  Существующая paired-связь сохраняется. Повторно запустить Setup EXE той же
  или следующей версии: один Run-value и один запущенный agent; после reboot
  Windows запускается эта же версия.

## 1b. Полевые исправления v0.2.5

- [ ] В одной вкладке Edge запустить два разных ролика подряд: новая обложка
  появляется, а предыдущая не остаётся во время загрузки новой.
- [ ] Перейти между двумя Edge-вкладками с разными роликами и повторить проверку.
- [ ] На главном экране Android включить ролик на 30–60 секунд: время и ползунок
  движутся плавно. Pause останавливает время, а seek сразу переставляет его.
- [ ] В Setup проверить доступную страницу выбора каталога, постоянный ярлык в
  меню Пуск и включённую по умолчанию задачу ярлыка на рабочем столе. Повторный
  Update сохраняет ранее выбранный каталог.
- [ ] После установки v0.2.6 открыть **Пуск** и найти
  **Bentley Remote → Bentley Remote Agent**.
- [ ] На Android центральная кнопка во время Playing показывает одноцветную
  векторную Pause-иконку в той же outline-кнопке, что Play/Previous/Next; жёлтого
  emoji-символа нет.
- [ ] На уже paired телефоне выполнить обычную перезагрузку, не делая Force Stop:
  после разблокировки Bentley сам показывает постоянное статусное уведомление
  «Restoring…», затем «Connected to DELL» либо «Waiting…», и восстанавливает
  LAN-соединение без открытия Activity. Сохранить результат отдельно для Android
  14 и ниже и для Android 15+.
- [ ] На Android 15+ у статуса нет Media3 player-card до первого открытия Bentley:
  это намеренно, потому что `mediaPlayback` foreground service запрещён прямо из
  BOOT_COMPLETED. Нажать уведомление: обычная Activity должна открыться, принять
  соединение от boot-service и восстановить стандартную media-card без ручного
  pairing. Battery → Unrestricted обязателен; режим Restricted может не доставить
  BOOT_COMPLETED.
- [ ] После обновления APK поверх уже paired версии не открывать Activity и
  проверить тот же старт `connectedDevice` service по `MY_PACKAGE_REPLACED`.

## 2. Установка и базовый UI — реальный A56

- [ ] Установить debug APK вручную/ADB; приложение запускается без crash.
- [ ] Разрешить notifications; проверить, что отказ не ломает основной экран.
- [ ] Страница показывает Disconnected, поле шестизначного кода Windows и
  неактивные media/power controls.
- [ ] Выбрать Battery → Unrestricted и убрать приложение из Deep sleeping apps.

## 3. Сетевые сценарии v0.2.x — обязательно на реальном оборудовании

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

### D. Перезагрузка Windows в стороннем Wi-Fi / hotspot

- [ ] Galaxy и Windows — клиенты одного стороннего Wi-Fi/hotspot; gateway не
  является Galaxy. Убедиться, что пара уже существует.
- [ ] Перезагрузить Windows, не трогая телефон и не перезапуская agent вручную.
- [ ] После входа в Windows agent запускается автоматически. В tray сначала
  допустим статус инициализации media API, но он не должен останавливать LAN
  reconnect: видны попытки last-known/gateway и `signed UDP broadcast`.
- [ ] Авторизованная сессия восстанавливается без ручного IP и без ручного restart
  agent в разумный срок (целевой ориентир — до 60 секунд после готовности Wi-Fi).
- [ ] Повторить обычный ручной restart agent как контроль: он также восстанавливает
  сессию. Проверить, что Galaxy-hotspot сценарий C не ухудшился.

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
- [ ] После переключения Edge на другую медиавкладку обновляются title, duration и art;
  обложка закрытой прежней вкладки не остаётся. Вернуться на предыдущую вкладку
  и повторить без перезапуска Windows agent.
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
- [ ] После **accepted** Shutdown или Restart карточка Bentley Remote и remote
  volume panel исчезают сразу, не дожидаясь reconnect timeout. Pairing не удалён.
- [ ] При отменённой/отклонённой команде или отсутствии ответа карточка остаётся,
  пока не сработает обычная проверка связи.

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
- [ ] Физически выключить ПК либо отключить Windows agent без команды из телефона:
  при полученном TCP close дождаться 15 секунд непрерывной недоступности; при
  «тихом» отключении дождаться не более 35 секунд без authenticated heartbeat.
  После этого карточка и remote-volume должны исчезнуть. Восстановить ПК/agent:
  после authenticated connection они должны появиться снова.
- [ ] На коротком отключении Wi-Fi Windows менее 15 секунд и последующем reconnect
  карточка не исчезает и управление продолжает работать.

## 9. Дополнительные настройки подключения (редкий fallback Android → Windows)

- [ ] На основном экране fallback/IP не отображается. Открыть меню `⋮` в блоке
  «Управление компьютером» → «Дополнительные настройки подключения».
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

## 11. Неформальное наблюдение совместимости

- Пользователь вручную сообщил о стабильной работе Bentley Remote на Galaxy S6
  с прошивкой от S8. Это полезное наблюдение, но не формальная сертификация;
  версия Android/One UI и номер сборки будут добавлены после уточнения устройства.
