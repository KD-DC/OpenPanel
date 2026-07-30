#ifndef AppVersion
  #error AppVersion must be supplied with /DAppVersion=x.y.z
#endif

#ifndef PublishDir
  #error PublishDir must be supplied with /DPublishDir=path
#endif

#ifndef OutputDir
  #error OutputDir must be supplied with /DOutputDir=path
#endif

#define AppName "OpenPanel"
#define AppExeName "OpenPanel.Host.exe"
#define AppPublisher "OpenPanel contributors"
#define AppUrl "https://github.com/KD-DC/OpenPanel"

[Setup]
AppId={{F9717D51-90F0-4A38-B95F-43C2A75AC29F}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=OpenPanel-Setup-{#AppVersion}
SetupIconFile=..\src\OpenPanel.Host\Assets\OpenPanel.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
MinVersion=10.0.17763
LicenseFile=..\LICENSE

[Tasks]
Name: "startup"; Description: "Start OpenPanel when I sign in"; GroupDescription: "Additional options:"; Flags: unchecked
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\OpenPanel"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall OpenPanel"; Filename: "{uninstallexe}"
Name: "{autodesktop}\OpenPanel"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "OpenPanel"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch OpenPanel"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent
