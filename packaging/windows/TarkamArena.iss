; Tarkam Arena — Windows installer (Inno Setup 6)
; Build: iscc /DAppVersion=0.1.0 /DSourceDir=..\..\out\win-x64 packaging\windows\TarkamArena.iss
; Output: out\installers\TarkamArena.Setup.exe

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\out\win-x64"
#endif

[Setup]
AppId={{6C1C7E2A-6E1E-4D7B-9C1A-7A2B5C9D4E11}
AppName=Tarkam Arena
AppVersion={#AppVersion}
AppVerName=Tarkam Arena {#AppVersion}
AppPublisher=Syubadubin Studios
AppPublisherURL=https://github.com/SyubadubinStudios/TarkamArena
AppCopyright=Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios
DefaultDirName={autopf}\Syubadubin Studios\Tarkam Arena
DefaultGroupName=Tarkam Arena
OutputDir=..\..\out\installers
OutputBaseFilename=TarkamArena.Setup
SetupIconFile=..\..\assets\ui\icon.ico
UninstallDisplayIcon={app}\TarkamArena.exe
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog
LicenseFile=..\..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Tarkam Arena"; Filename: "{app}\TarkamArena.exe"
Name: "{group}\{cm:UninstallProgram,Tarkam Arena}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Tarkam Arena"; Filename: "{app}\TarkamArena.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\TarkamArena.exe"; Description: "{cm:LaunchProgram,Tarkam Arena}"; Flags: nowait postinstall skipifsilent
