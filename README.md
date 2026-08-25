# Deskora

Локальная связка Android + Windows 11 для управления системной медиасессией и
общей громкостью Windows с Samsung Galaxy A56. Облака, аккаунтов, аналитики,
relay-серверов и браузерного расширения нет.

MVP реализует:

- автоматическое подключение Windows к IP текущего Wi-Fi default gateway — в
  основном сценарии это Galaxy, раздающий hotspot;
- WebSocket `ws://<gateway>:45892/bentley`, heartbeat и переподключение с
  ограниченным exponential backoff;
- резервное подключение Android → Windows на TCP `45893`;
- Windows GSMTC (`Windows.Media.Control`) для Edge/YouTube и других приложений;
- Windows Core Audio (NAudio): master volume и mute;
- Android Media3 `MediaSessionService` и настоящий remote `Player`, который не
  воспроизводит звук на телефоне;
- системную медиакарточку с title, artist, состоянием, timeline и artwork;
- play, pause, previous, next, ±10 секунд, seek, volume и mute;
- одноразовый шестизначный код, Android Keystore, Windows DPAPI и HMAC-SHA256
  для всех последующих соединений;
- tray-агент без постоянно открытого окна и опциональный автозапуск.
- карточку подключённого ПК: одноразовый защищённый preview текущего primary
  desktop при открытии приложения или по кнопке Refresh; это не видеострим и
  не удалённый рабочий стол.

## Структура

```text
android/                 Kotlin + Compose + Media3 APK
windows/                 .NET 8 WinForms tray agent
protocol/                описание, JSON Schema и примеры protocol v1
docs/ARCHITECTURE.md      архитектура и сетевые решения
scripts/                  проверка протокола и настройка fallback
releases/                 устанавливаемые тестовые сборки по SemVer
TESTING.md                чек-лист Galaxy A56 / One UI 8.5
```

## Версионирование и releases

Текущая версия хранится в корневом файле `VERSION` и автоматически применяется
к Android и Windows. Стабильные тестовые сборки помечаются аннотированными Git
tag вида `v0.1.0`, `v0.1.1`, `v0.2.0`.

Каждая реально устанавливаемая версия находится в `releases/vX.Y.Z/` и содержит
только versioned APK, Windows Setup EXE и краткий `CHANGELOG.md`. Копии
исходников туда не кладутся: их история хранится в Git.
Полный порядок выпуска описан в [docs/VERSIONING.md](docs/VERSIONING.md), а
обязательные правила для дальнейшей работы — в [AGENTS.md](AGENTS.md).

## Автоматическая portable-сборка

Из обычного PowerShell в корне репозитория выполните:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  .\scripts\bootstrap-portable-and-package.ps1
```

Скрипт не устанавливает SDK в систему: .NET 8, Microsoft OpenJDK 17 и Android
SDK сохраняются в соседний каталог `work`. Результат — debug APK,
самодостаточная папка Windows x64, versioned ZIP и SHA-256 — появляется в
соседнем каталоге `outputs`.

## Требования

Windows-сборка:

- Windows 11 x64;
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0);
- интернет только для первого `dotnet restore` (пакеты NAudio и ProtectedData).

Android-сборка:

- Android Studio с JDK 17 или отдельный JDK 17;
- Android SDK Platform 35 и Build Tools;
- интернет для первой загрузки Gradle и Maven-зависимостей.

После сборки и сопряжения интернет для управления не нужен. Мобильные данные
могут быть выключены, пока локальная hotspot-связь телефон ↔ ПК остаётся жива.

## Установка и обновление Windows-агента

Откройте `BentleyRemote-Setup-vX.Y.Z.exe` из текущего релиза двойным щелчком,
подтвердите стандартный запрос Windows UAC и нажмите **Install** (или
**Update**). Установщик сам останавливает только Bentley Remote Agent,
устанавливает актуальную версию в `C:\Program Files\Bentley Remote\`, заменяет
единственную известную запись автозапуска Bentley Remote и запускает обновлённый
агент. Автозапуск хранится как одна запись Bentley Remote в HKLM и действует для
всех пользователей этого компьютера. Повторный запуск Setup EXE новой версии —
обычный способ обновления.

На странице установки можно выбрать другой каталог; при обновлении сохраняется
ранее выбранный. Установщик всегда создаёт ярлык в меню Пуск и предлагает
включённый по умолчанию ярлык на рабочем столе.

Pairing/config остаётся в `%LOCALAPPDATA%\BentleyRemote` и поэтому сохраняется
при обновлении. В tray выберите **About / diagnostics…**, чтобы увидеть версию и
фактический путь запущенного EXE; он должен указывать на Program Files.

## Сборка и portable-запуск Windows-агента

Из PowerShell в корне репозитория:

```powershell
dotnet restore .\windows\BentleyRemote.sln
dotnet build .\windows\BentleyRemote.sln -c Release
dotnet run --project .\windows\src\BentleyRemote.Agent\BentleyRemote.Agent.csproj -c Release
```

Публикация самостоятельной папки (на ПК всё равно должен быть .NET 8 Desktop
Runtime, но SDK уже не нужен):

```powershell
dotnet publish .\windows\src\BentleyRemote.Agent\BentleyRemote.Agent.csproj `
  -c Release -r win-x64 --self-contained false `
  -o .\artifacts\windows-x64
```

Portable build предназначен только для разработки и диагностики, но не является
способом пользовательского обновления. Иконка появится в system tray. В меню
доступны статус, pairing, About/diagnostics, список найденных gateway,
автозапуск и выход. Автозапуск указывает только на установленный EXE в Program
Files.

## Сборка APK

`android\gradlew.bat` — небольшой bootstrap: при первом запуске он загружает
официальный Gradle 8.11.1 в локальную `android\.gradle-dist`, затем запускает
обычную сборку.

```powershell
cd .\android
.\gradlew.bat :app:assembleDebug
```

APK: `android\app\build\outputs\apk\debug\app-debug.apk`.

Для release APK создайте signing config/keystore в Android Studio. Для личной
ручной установки debug APK достаточно; он не предназначен для Google Play.

## Установка на Galaxy A56

Через ADB:

```powershell
adb install -r .\android\app\build\outputs\apk\debug\app-debug.apk
```

Или скопируйте APK на телефон, откройте его в «Мои файлы» и разово разрешите
этому источнику «Установка неизвестных приложений».

При первом запуске:

1. Разрешите уведомления — без них системная медиакарточка/foreground-индикация
   могут быть скрыты.
2. Откройте показанную приложением страницу battery optimization. В One UI
   найдите Bentley Remote в **Настройки → Приложения → Bentley Remote →
   Батарея** и выберите **Без ограничений / Unrestricted**.
3. Проверьте, что Bentley Remote не добавлен в **Обслуживание устройства →
   Батарея → Ограничения фонового использования → Приложения в глубоком сне**.
   Названия пунктов могут немного отличаться в конкретной сборке One UI 8.5.
4. Не блокируйте уведомление медиасессии.

Приложение не запрашивает доступ к контактам, файлам, геопозиции или интернету
за пределами локального WebSocket. Разрешение `INTERNET` в Android также нужно
для обычных локальных сокетов.

## Первое сопряжение в основном hotspot-сценарии

1. На Galaxy включите мобильную точку доступа WPA2/WPA3.
2. Подключите Windows-компьютер к Wi-Fi этой точки доступа.
3. Запустите Bentley Remote на телефоне. На карточке Secure pairing появится
   код из 6 цифр, действующий 10 минут.
4. Запустите Windows-агент. Он сам переберёт IPv4 default gateway активных
   интерфейсов, отдавая приоритет Wi-Fi; IP не хардкодится.
5. В tray выберите **Pair with phone…** и введите код телефона.
6. Дождитесь `Connected` на телефоне и в tray. Секрет останется в Android
   Keystore, а на Windows — в защищённом DPAPI-файле текущего пользователя.

Первичное сообщение передаёт секрет по локальному `ws://`, поэтому выполняйте
pairing только на своей защищённой hotspot-сети без посторонних клиентов.
Последующие соединения используют HMAC с timestamp и одноразовым nonce.

Диагностика gateway/порта на Windows:

```powershell
Get-NetIPConfiguration | Where-Object IPv4DefaultGateway
$gateway = (Get-NetIPConfiguration | Where-Object IPv4DefaultGateway | Select-Object -First 1).IPv4DefaultGateway.NextHop
Test-NetConnection $gateway -Port 45892
```

`TcpTestSucceeded: True` означает, что One UI пропускает входящее соединение к
Android-приложению на hotspot-интерфейсе.

## Проверка Edge / YouTube

1. Откройте обычное (не InPrivate) окно Microsoft Edge и запустите YouTube.
2. Убедитесь, что аппаратная кнопка Play/Pause на клавиатуре управляет роликом —
   это простой признак опубликованной системной медиасессии.
3. Bentley Remote должен получить title/status/timeline; Android Media3 создаст
   системную карточку. Play/Pause и громкость должны менять состояние Windows.

Некоторые ролики/реклама/страницы YouTube не публикуют previous, next, seek или
thumbnail. Агент передаёт capability-флаги Windows и отключает неподдерживаемые
кнопки; расширение Edge намеренно не входит в MVP.

## Резервный режим Android → Windows

Fallback нужен только если `Test-NetConnection <gateway> -Port 45892` стабильно
не проходит. Сначала проведите обычное сопряжение хотя бы один раз — fallback
не передаёт новый pairing-secret.

1. От имени администратора выполните:

   ```powershell
   .\scripts\enable-windows-reverse-mode.ps1
   ```

   Скрипт создаёт URL ACL для `http://+:45893/bentley/` текущему пользователю и
   входящее правило Windows Firewall только для профиля Private.

2. Найдите IPv4 компьютера на hotspot-интерфейсе через `Get-NetIPAddress
   -AddressFamily IPv4`.
3. В Android включите fallback, введите этот IPv4 без `ws://` и нажмите Apply.

Основной и fallback-каналы взаимозаменяемы после `auth.ok`; одновременно
активным считается последний аутентифицированный канал.

## Хранилища и сброс

- Android: AES-256/GCM key в Android Keystore, шифротекст в private
  SharedPreferences без backup.
- Windows: `%LOCALAPPDATA%\BentleyRemote\agent-config.json`; секрет внутри
  защищён DPAPI CurrentUser.
- «Forget computer» на телефоне и «Re-pair» в tray удаляют соответствующую
  сторону. Для полного сброса нажмите оба пункта и проведите pairing заново.

## Известные ограничения MVP

- Надёжность входящего сокета на интерфейсе Samsung hotspot, поведение Media3
  remote volume и физические кнопки нельзя подтвердить без реального A56.
- Media3 переводит удалённый `DeviceInfo` в Android remote volume API, но One UI
  сама решает, когда физические клавиши направляются активной remote session.
- Android может завершить приостановленную `MediaSessionService`; при активном
  воспроизведении Media3 переводит её в foreground. Режим Unrestricted важен.
- Windows выбирает `GetCurrentSession()`. Если одновременно активны несколько
  системных медиасессий, приоритет определяет Windows.
- MVP — одна пара телефон ↔ ПК. Для нового ПК нужно сбросить pairing.
- `ws://` скрывает не содержимое/метаданные. HMAC аутентифицирует стороны, но не
  шифрует локальный трафик. Для личной защищённой hotspot-сети это осознанный
  MVP-компромисс.
- Desktop preview отдельно шифруется AES-256-GCM ключом, выведенным из secret
  pairing, имеет одноразовый nonce и лимит 1 MiB. Он доступен только уже
  paired устройству; скриншоты не сохраняются и не передаются в облако.

Полный аппаратный чек-лист находится в [TESTING.md](TESTING.md).
Фактический статус проверок текущего окружения записан в
[BUILD_STATUS.md](BUILD_STATUS.md).
