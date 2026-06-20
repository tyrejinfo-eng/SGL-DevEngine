; SGL DevEngine Beta 1.1.6 - Inno Setup Installer Script
; Build: April 9, 2026
; Platform: Windows 10/11 (x64)

[Setup]
AppName=SGL DevEngine
AppVersion=1.1.6
AppPublisher=SGL DevEngine Team
AppPublisherURL=https://github.com/sgldevengine/sgldevengine
AppSupportURL=https://github.com/sgldevengine/sgldevengine/issues
AppUpdatesURL=https://github.com/sgldevengine/sgldevengine/releases
DefaultDirName={autopf}\SGL DevEngine
DefaultGroupName=SGL DevEngine
OutputDir=.
OutputBaseFilename=SGLDevEngine-Setup-1.1.6
SourceDir=.
Compression=lzma2
SolidCompression=yes
AllowNetworkDrive=no
AllowUNCPath=no
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
UsedUserAreasWarning=no
WizardStyle=modern
CloseApplicationsFilter=*.exe
MinVersion=10.0.19041
VersionInfoVersion=1.1.6.0
VersionInfoCompany=SGL DevEngine
VersionInfoProductName=SGL DevEngine
VersionInfoProductVersion=1.1.6.0
VersionInfoFileVersion=1.1.6.0
VersionInfoFileDescription=SGL DevEngine - Visual Blueprint Architecture Platform

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIconTask}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunch"; Description: "{cm:CreateQuickLaunchIconTask}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked and SkipIfDoesntExist

[Files]
; Main executables and DLLs
Source: "SGL DevEngine Windows Quick Deploy Version\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Documents\BUSINESS-PLAN.md"; DestDir: "{app}\Documents"; Flags: ignoreversion
Source: "Audit Review\*.md"; DestDir: "{app}\Audit"; Flags: ignoreversion

[Icons]
Name: "{group}\SGL DevEngine"; Filename: "{app}\SGLDevEngine.Studio.exe"; Comment: "Visual Blueprint Architecture Platform"; IconFilename: "{app}\SGLDevEngine.Studio.exe"; IconIndex: 0
Name: "{group}\Documentation"; Filename: "{app}\Audit\BETA-1.1.6-COMPREHENSIVE-AUDIT.md"; Comment: "System Documentation"
Name: "{group}\{cm:UninstallProgram,SGL DevEngine}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\SGL DevEngine"; Filename: "{app}\SGLDevEngine.Studio.exe"; Tasks: desktopicon; Comment: "Visual Blueprint Architecture Platform"; IconFilename: "{app}\SGLDevEngine.Studio.exe"; IconIndex: 0
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\SGL DevEngine"; Filename: "{app}\SGLDevEngine.Studio.exe"; Tasks: quicklaunch; IconFilename: "{app}\SGLDevEngine.Studio.exe"; IconIndex: 0

[Run]
Filename: "{app}\SGLDevEngine.Studio.exe"; Description: "{cm:LaunchProgram,SGL DevEngine}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel1.Caption := 'Welcome to SGL DevEngine Setup';
  WizardForm.WelcomeLabel2.Caption := 'This will install SGL DevEngine Beta 1.1.6' + #13 + #13 +
    'Visual Blueprint Architecture Platform with Real AI Integration' + #13 +
    'Click Next to continue, or Cancel to exit the setup.';
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
  begin
    WizardForm.FinishedHeadingLabel.Caption := 'SGL DevEngine Installation Complete';
    WizardForm.FinishedLabel.Caption := 'SGL DevEngine Beta 1.1.6 has been successfully installed!' + #13 + #13 +
      'You can now:' + #13 +
      '• Design blueprints in the visual editor' + #13 +
      '• Generate code (C#, Python, C++)' + #13 +
      '• Execute against real databases' + #13 +
      '• Integrate with Claude, OpenAI, or local models' + #13 +
      '• Connect to GitHub repositories' + #13 + #13 +
      'For documentation, see the "Audit" folder in the installation directory.';
  end;
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  MsgBox('SGL DevEngine Beta 1.1.6' + #13 + #13 +
    'System Requirements:' + #13 +
    '• Windows 10 (build 19041) or later' + #13 +
    '• .NET 8.0 Runtime or later' + #13 +
    '• 500 MB free disk space' + #13 +
    '• Internet connection for LLM providers' + #13 + #13 +
    'Click OK to proceed with installation.',
    mbInformation, MB_OK);
end;
