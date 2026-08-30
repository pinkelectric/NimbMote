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
CloseApplications=yes
RestartApplications=no
UninstallDisplayName=Deskora Agent
UninstallDisplayIcon={app}\{#AppExeName}

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; Flags: checkedonce

[Icons]
; Use the explicit common Start-menu folder. {group} depends on shell/group-page
; resolution and was not discoverable after a clean v0.2.5 installation.
Name: "{commonprograms}\Deskora\Deskora Agent"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\Deskora"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; Per-machine startup launches the installed agent for the signed-in user. It
; avoids an elevated installer writing a Run value into the wrong HKCU hive.
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Deskora"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExeName}"; Parameters: "--show-pairing"; Flags: nowait skipifsilent; Check: IsFirstDeskoraInstall
Filename: "{app}\{#AppExeName}"; Flags: nowait skipifsilent; Check: not IsFirstDeskoraInstall

[Code]
function IsFirstDeskoraInstall: Boolean;
begin
  { This checks the stable AppId before Inno writes the new version's uninstall key.
    Updating an existing Deskora installation must stay quiet. }
  Result := not RegKeyExists(HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{B6BFE4C7-B8E5-4F77-96AB-50D0511F4295}_is1');
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
