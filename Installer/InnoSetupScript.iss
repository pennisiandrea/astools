#define MyAppName "ASTools"
#define MyAppVersion "1.0"
#define MyAppPublisher "Andrea Pennisi"
#define DotNETInstaller "windowsdesktop-runtime-8.0.8-win-x64.exe"
#define DotNETVersion "Microsoft.WindowsDesktop.App 8.0.8"

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
OutputBaseFilename=ASToolsInstaller
SetupIconFile=C:\Data\astools\Data\Resources\Icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Dependencies
Source: "Dependencies\{#DotNETInstaller}"; Flags: dontcopy noencryption
; Projects
Source: "C:\Data\astools\ASTools.Core\bin\Release\net8.0-windows\*"; DestDir: "{app}\ASTools.Core"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Data\astools\ASTools.UI\bin\Release\net8.0-windows\*"; DestDir: "{app}\ASTools.UI"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Data\astools\ASTools.Library\bin\Release\net8.0-windows\*"; DestDir: "{app}\ASTools.Library"; Flags: ignoreversion recursesubdirs createallsubdirs
; Data
Source: "C:\Data\astools\Data\ASTemplates\Default\*"; DestDir: "{commonappdata}\ASTools\ASTemplates\Default"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Data\astools\Data\Config.ini"; DestDir: "{commonappdata}\ASTools"; Flags: ignoreversion onlyifdoesntexist
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[INI]
Filename: "{commonappdata}\ASTools\Config.ini"; Section: "Templates"; Key: "Repository1"; String: "Default|{commonappdata}\ASTools\ASTemplates\Default"; Flags: createkeyifdoesntexist

[Registry]
;Registry data from file RegistryKeys.reg
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates"; ValueType: string; ValueName: ""; ValueData: "Open ASTemplates here"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\ASTools.UI\ASTools.UI.exe"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates\command"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Directory\background\shell\ASTemplates\command"; ValueType: string; ValueName: ""; ValueData: """{app}\ASTools.UI\ASTools.UI.exe"" ""--page"" ""templates"" ""--working-dir"" ""%V"""; Flags: uninsdeletevalue
Root: HKLM; Subkey: "SOFTWARE\ASTools"; Flags: uninsdeletekey; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\ASTools"; ValueType: string; ValueName: "ConfigFile"; ValueData: "{commonappdata}\ASTools\Config.ini"; Flags: uninsdeletevalue; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\ASTools"; ValueType: string; ValueName: "LogError"; ValueData: "{commonappdata}\ASTools\Errors.log"; Flags: uninsdeletevalue; Check: Is64BitInstallMode
Root: HKLM; Subkey: "SOFTWARE\ASTools"; ValueType: string; ValueName: "CorePath"; ValueData: "{app}\ASTools.Core\ASTools.Core.exe"; Flags: uninsdeletevalue; Check: Is64BitInstallMode
;End of registry data from file RegistryKeys.reg

[Icons]
Name: "{group}\ASTools.Core"; Filename: "{app}\ASTools.Core\ASTools.Core.exe"
Name: "{group}\ASTools.UI"; Filename: "{app}\ASTools.UI\ASTools.UI.exe"

[Code]
// Dependencies check code 
// Check if dotnet runtime correct version is installed
function IsDotNetInstalled(Version: string): Boolean;
var
  TmpFileName: string;
  ResultString: string;
  ExecStdout: AnsiString;
  ResultCode: Integer;
begin
  TmpFileName := ExpandConstant('{tmp}') + '\dotnetlist_results.txt';
  // Execute dotnet --list-runtimes command in order to get runtimes list
  Result := Exec('cmd.exe', '/C dotnet --list-runtimes > "' + TmpFileName + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if not Result then 
    Exit;
    
  LoadStringFromFile(TmpFileName,ExecStdout);
  
  ResultString := ExecStdout;
  
  DeleteFile(TmpFileName);
  
  // Check if Version is among command output
  Result := (Pos(Version, ResultString) > 0);
end;

// Install dotnet if needed
function InstallDotNetIfNeeded: Boolean;
var
  ResultCode: Integer;
begin
  if not IsDotNetInstalled(ExpandConstant('{#DotNETVersion}')) then
  begin
    ExtractTemporaryFiles('{tmp}\' + ExpandConstant('{#DotNETInstaller}'));
    
    Result := Exec(ExpandConstant('{tmp}\{#DotNETInstaller}'), '', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
  end
  else
  begin
    Result := True; // .NET is already installed
  end;
end;

function InitializeSetup: Boolean;
begin

  InstallDotNetIfNeeded();
  
  Result := True;
end;
