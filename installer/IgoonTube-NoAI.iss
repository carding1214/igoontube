#define DistRoot "F:\PUPlayer\dist"

[Setup]
AppId={{DA49A745-0E8E-46B6-B783-390403058B4D}
AppName=IgoonTube NoAI
AppVersion=1.0.0
AppPublisher=IgoonTube
SetupIconFile=..\src\PUPlayer.App\Assets\IgoonTube.ico
DefaultDirName=F:\IgoonTube-NoAI
DefaultGroupName=IgoonTube NoAI
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
OutputBaseFilename=IgoonTube-NoAI-Setup
UninstallDisplayIcon={app}\IgoonTube.exe
ChangesAssociations=yes
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "associate"; Description: "Use IgoonTube NoAI as the default player for supported formats"; Flags: unchecked

[Files]
Source: "{#DistRoot}\IgoonTube-NoAI\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
Root: HKCU; Subkey: "Software\Classes\IgoonTube.NoAI.Video"; ValueType: string; ValueData: "IgoonTube NoAI media"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\IgoonTube.NoAI.Video\DefaultIcon"; ValueType: string; ValueData: "{app}\IgoonTube.exe,0"
Root: HKCU; Subkey: "Software\Classes\IgoonTube.NoAI.Video\shell\open\command"; ValueType: string; ValueData: """{app}\IgoonTube.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\.mp4"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.mkv"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.webm"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.avi"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.mov"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.m4v"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.mp3"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.flac"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.wav"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.m4a"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.ogg"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.opus"; ValueType: string; ValueData: "IgoonTube.NoAI.Video"; Tasks: associate; Flags: uninsdeletevalue
