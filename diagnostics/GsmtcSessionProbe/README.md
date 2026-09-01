# GSMTC Session Probe

Отдельная диагностическая утилита для исследования результата
`GlobalSystemMediaTransportControlsSessionManager.GetSessions()`. Она не подключена
к Windows-агенту, Android-приложению, release-сборке или протоколу Bentley Remote.

## Сборка

```powershell
dotnet build .\diagnostics\GsmtcSessionProbe\GsmtcSessionProbe.csproj -c Debug
```

## Снимок всех сессий

```powershell
dotnet run --project .\diagnostics\GsmtcSessionProbe\GsmtcSessionProbe.csproj --no-build
```

## Наблюдение за событиями

```powershell
dotnet run --project .\diagnostics\GsmtcSessionProbe\GsmtcSessionProbe.csproj --no-build -- `
  --watch 600 --poll 5 --log .\work\diagnostics\gsmtc-edge.log
```

Probe выводит появление и исчезновение сессий, изменение текущей сессии,
metadata, playback state и timeline. `ProbeId` и `RuntimeReference` позволяют
увидеть, сохраняется ли тот же объект при смене активной вкладки и metadata.

## Адресная команда

Индекс берётся из снимка, который утилита печатает непосредственно перед
командой. Без `--session` и `--action` утилита работает строго read-only.

```powershell
dotnet run --project .\diagnostics\GsmtcSessionProbe\GsmtcSessionProbe.csproj --no-build -- `
  --session 2 --action play --watch 5
```

Поддерживаются `play`, `pause`, `previous`, `next` и `seek`. Для `seek` нужно
добавить `--position-seconds <число>`.
