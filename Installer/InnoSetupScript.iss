; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "ASTools"
#define MyAppVersion "1.0"
#define MyAppPublisher "Andrea Pennisi"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{288E2436-A6DF-4AD8-B9CC-05C0E8E57401}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} V{#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
PrivilegesRequired=admin
OutputDir=C:\Data\astools\Installer
OutputBaseFilename=ASTools
SetupIconFile=C:\Data\astools\Data\Resources\Icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "C:\Data\astools\ASTools.Core\bin\Release\net8.0\win-x64\*"; DestDir: "{app}\ASTools.Core"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Data\astools\ASTools.UI\bin\Release\net8.0-windows\win-x64\*"; DestDir: "{app}\ASTools.UI"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Data\astools\Data\ASTemplates\Default\*"; DestDir: "{sd}\ASTools\ASTemplates\Default"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Data\astools\Data\Config.ini"; DestDir: "{sd}\ASTools"; Flags: ignoreversion; Check: not FileExists('{sd}\ASTools\Config.ini')
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[INI]
Filename: "{sd}\ASTools\Config.ini"; Section: "Templates"; Key: "Repository1"; String: "{sd}\ASTools\ASTemplates\Default"; Check: not FileExists('{sd}\ASTools\Config.ini')

[Registry]
;Registry data from file RegistryKeys.reg
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates"; ValueType: string; ValueName: ""; ValueData: "Open ASTemplates here"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\ASTools.UI\ASTools.UI.exe"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates\command"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates\command"; ValueType: string; ValueName: ""; ValueData: """{app}\ASTools.UI\ASTools.UI.exe"" ""--page"" ""templates"" ""--working-dir"" ""%V"""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "SOFTWARE\ASTools"; Flags: uninsdeletekey; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\ASTools"; ValueType: string; ValueName: "ConfigFile"; ValueData: "{sd}\ASTools\Config.ini"; Flags: uninsdeletevalue; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\ASTools"; ValueType: string; ValueName: "LogError"; ValueData: "{sd}\ASTools\Errors.log"; Flags: uninsdeletevalue; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\ASTools"; ValueType: string; ValueName: "CorePath"; ValueData: "{app}\ASTools.Core\ASTools.Core.exe"; Flags: uninsdeletevalue; Check: Is64BitInstallMode
;End of registry data from file RegistryKeys.reg

[Icons]
Name: "{group}\ASTools.Core"; Filename: "{app}\ASTools.Core\ASTools.Core.exe"
Name: "{group}\ASTools.UI"; Filename: "{app}\ASTools.UI\ASTools.UI.exe"
