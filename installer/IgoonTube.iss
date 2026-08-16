#define DistRoot "F:\PUPlayer\dist"

[Setup]
AppId={{8E2C8764-9A21-4F52-9A90-2DA45A45A7CB}
AppName=IgoonTube
AppVersion=1.0.0
AppPublisher=IgoonTube
SetupIconFile=..\src\PUPlayer.App\Assets\IgoonTube.ico
DefaultDirName=F:\IgoonTube
DefaultGroupName=IgoonTube
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern dynamic
Compression=lzma2
SolidCompression=yes
DiskSpanning=yes
DiskSliceSize=max
OutputDir={#DistRoot}
OutputBaseFilename=IgoonTube-Setup
UninstallDisplayIcon={app}\IgoonTube.exe
ChangesAssociations=yes
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "associate"; Description: "Usar IgoonTube como reproductor predeterminado para los formatos compatibles"; Flags: unchecked

[Files]
Source: "{#DistRoot}\IgoonTube\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
Root: HKCU; Subkey: "Software\Classes\IgoonTube.Video"; ValueType: string; ValueData: "Multimedia de IgoonTube"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\IgoonTube.Video\DefaultIcon"; ValueType: string; ValueData: "{app}\IgoonTube.exe,0"
Root: HKCU; Subkey: "Software\Classes\IgoonTube.Video\shell\open\command"; ValueType: string; ValueData: """{app}\IgoonTube.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\shell\open\command"; ValueType: string; ValueData: """{app}\IgoonTube.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".mp4"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".mkv"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".webm"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".avi"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".mov"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".m4v"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".mp3"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".flac"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".wav"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".m4a"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".ogg"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\Applications\IgoonTube.exe\SupportedTypes"; ValueType: string; ValueName: ".opus"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\.mp4"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.mkv"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.webm"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.avi"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.mov"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.m4v"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.mp3"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.flac"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.wav"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.m4a"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.ogg"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.opus"; ValueType: string; ValueData: "IgoonTube.Video"; Tasks: associate; Flags: uninsdeletevalue
