#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

#define AppName "截图助手"
#define AppExecutable "ScreenshotAssistant.App.exe"

[Setup]
AppId={{9DD84909-215D-46CF-91D8-F3C0E32BB7AF}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=SnapAnnotate contributors
AppPublisherURL=https://github.com/chendawang/SnapAnnotate
AppSupportURL=https://github.com/chendawang/SnapAnnotate/issues
AppUpdatesURL=https://github.com/chendawang/SnapAnnotate/releases
DefaultDirName={localappdata}\Programs\ScreenshotAssistant
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
OutputDir=..\dist
OutputBaseFilename=ScreenshotAssistant-Setup-{#AppVersion}-win-x64
SetupIconFile=..\src\ScreenshotAssistant.App\Assets\ScreenshotAssistant.ico
UninstallDisplayIcon={app}\{#AppExecutable}
UninstallDisplayName={#AppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany=SnapAnnotate
VersionInfoDescription=截图助手安装程序
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式："; Flags: checkedonce

[Files]
Source: "..\.artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: files; Name: "{app}\Uninstall.ps1"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExecutable}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExecutable}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExecutable}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExecutable}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExecutable}"; Description: "启动{#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
    RegDeleteKeyIncludingSubkeys(
      HKEY_CURRENT_USER,
      'Software\Microsoft\Windows\CurrentVersion\Uninstall\ScreenshotAssistant');
end;
