; Deskora — native interactive installer, compiled by Inno Setup 6.
; Build defines required: SourceDir, OutputDir, ProductVersion.

#ifndef SourceDir
  #error SourceDir build define is required
#endif
#ifndef OutputDir
  #error OutputDir build define is required
#endif
#ifndef ProductVersion
  #error ProductVersion build define is required
#endif

#define AppName "Deskora"
#define AppExeName "BentleyRemote.Agent.exe"
#define AppId "{{B6BFE4C7-B8E5-4F77-96AB-50D0511F4295}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#ProductVersion}
AppPublisher=Deskora
DefaultDirName={autopf}\Deskora
DefaultGroupName=Deskora
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=Deskora-Setup-v{#ProductVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#SourceDir}\Deskora.ico
CloseApplications=yes
RestartApplications=no
UninstallDisplayName=Deskora Agent
UninstallDisplayIcon={app}\Deskora.ico

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; Flags: checkedonce

[Icons]
; Use the explicit common Start-menu folder. {group} depends on shell/group-page
; resolution and was not discoverable after a clean v0.2.5 installation.
Name: "{commonprograms}\Deskora\Deskora Agent"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\Deskora.ico"
Name: "{autodesktop}\Deskora"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\Deskora.ico"; Tasks: desktopicon

[Registry]
; Per-machine startup launches the installed agent for the signed-in user. It
; avoids an elevated installer writing a Run value into the wrong HKCU hive.
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Deskora"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExeName}"; Parameters: "--show-pairing"; Flags: nowait skipifsilent; Check: IsFirstDeskoraInstall
Filename: "{app}\{#AppExeName}"; Flags: nowait skipifsilent; Check: not IsFirstDeskoraInstall

[Code]
var
  RemoveUserData: Boolean;

function IsFirstDeskoraInstall: Boolean;
begin
  { This checks the stable AppId before Inno writes the new version's uninstall key.
    Updating an existing Deskora installation must stay quiet. }
  Result := not RegKeyExists(HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{B6BFE4C7-B8E5-4F77-96AB-50D0511F4295}_is1');
end;

function InitializeUninstall(): Boolean;
begin
  { Normal removal preserves pairing by default. The user can explicitly choose
    a clean slate for testing or handing the computer to another person. }
  RemoveUserData := False;
  if not UninstallSilent then
    RemoveUserData := MsgBox(
      'Удалить также сохранённое сопряжение и все данные Deskora?' + #13#10 + #13#10 +
      'Да — полное удаление: следующая установка будет как первая.' + #13#10 +
      'Нет — удалить только программу и оставить данные для будущей установки.',
      mbConfirmation, MB_YESNO) = IDYES;
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if (CurUninstallStep = usPostUninstall) and RemoveUserData then
    DelTree(ExpandConstant('{localappdata}\BentleyRemote'), True, True, True);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then begin
    { Stop only the known Deskora agent before replacing its own files. }
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM BentleyRemote.Agent.exe', '', SW_HIDE,
      ewWaitUntilTerminated, ResultCode);
  end;
  if CurStep = ssPostInstall then begin
    { Replace only the two known legacy shortcuts with their Deskora equivalents. }
    DeleteFile(ExpandConstant('{commonprograms}\Bentley Remote\Bentley Remote Agent.lnk'));
    DeleteFile(ExpandConstant('{autodesktop}\Bentley Remote.lnk'));
    { v0.1.1/v0.2.3 used per-user Run values. Remove only these known names;
      no third-party startup entry is enumerated or modified. }
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'Bentley Remote');
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'BentleyRemote.Agent');
    RegDeleteValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'Bentley Remote');
    RegDeleteValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'BentleyRemote.Agent');
  end;
end;
