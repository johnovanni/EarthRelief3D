; Inno Setup script for the EarthRelief3D installer (EarthRelief3D-Setup.exe).
; Built by .github/workflows/release.yml:  iscc /DAppVersion=1.0.1 /DPublishDir=<publish folder> /DOutputDir=<out> EarthRelief3D.iss
; Installs to C:\Program Files\Qapow\EarthRelief3D by default (every QAPOW program goes under C:\Program Files\Qapow),
; and the user can pick another folder. Settings, caches and logs stay in %LOCALAPPDATA%\EarthRelief3D.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\..\artifacts\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\artifacts\release"
#endif

#define AppName "EarthRelief3D"
#define AppExe "EarthRelief3D.App.exe"

[Setup]
; AppId identifies this program to Windows across versions. Never change it.
AppId={{D81FC340-0932-4471-8046-481ADD412859}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=John Apolloni
AppPublisherURL=https://github.com/johnovanni/EarthRelief3D
AppSupportURL=https://github.com/johnovanni/EarthRelief3D
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\Qapow\{#AppName}
DisableDirPage=no
DefaultGroupName=Qapow
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
SetupIconFile=..\..\src\EarthRelief3D.App\app.ico
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
CloseApplications=yes
; Restart Manager closes the app but does not start it again: the only restart is the silent-update entry of the Run section below, as the user (M5).
RestartApplications=no
OutputDir={#OutputDir}
OutputBaseFilename=EarthRelief3D-Setup

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[InstallDelete]
; Each install or update starts from a fresh data tree, so no file a newer version dropped is left behind (M8).
; Nothing the user keeps is here: settings, caches and logs are in %LOCALAPPDATA%\EarthRelief3D.
Type: filesandordirs; Name: "{app}\data"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Qapow\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
; The in-app updater runs this installer with /SILENT (P5-21): start the app again afterwards, as the user, not as administrator.
Filename: "{app}\{#AppExe}"; Flags: nowait runasoriginaluser skipifnotsilent
