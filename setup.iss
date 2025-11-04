; Inno Setup Script for Devanagari IME
; This creates an installer that installs the application and sets up Task Scheduler

#define MyAppName "Devanagari IME"
#define MyAppVersion "1.0"
#define MyAppPublisher "Devanagari IME"
#define MyAppExeName "DevanagariIME.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
OutputDir=installer
OutputBaseFilename=DevanagariIME-Setup
SetupIconFile=
Compression=lzma2
SolidCompression=yes
LZMAUseSeparateProcess=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
; Uninstaller settings - ensure it's properly registered
CreateUninstallRegKey=yes
UninstallDisplayIcon={app}\app.ico
UninstallDisplayName={#MyAppName}
UninstallFilesDir={app}
; Prevent duplicate installations
DisableDirPage=no
AllowRootDirectory=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Start Devanagari IME automatically on Windows login"; GroupDescription: "Startup Options"; Flags: checkedonce

[Files]
; Copy the self-contained executable (includes .NET runtime)
; With PublishSingleFile=true, everything is bundled into one EXE
Source: "bin\Release\net6.0-windows\win-x64\publish\DevanagariIME.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "install-task-scheduler.ps1"; DestDir: "{app}"; Flags: ignoreversion
; Include icon file for Windows app list
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
; Add uninstall shortcut to the installation folder for easy access
Name: "{app}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"; IconFilename: "{app}\{#MyAppExeName}"

[Run]
; Install Task Scheduler task (if "Start on login" is checked)
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -NoProfile -File ""{app}\install-task-scheduler.ps1"""; Description: "Set up automatic startup on login"; StatusMsg: "Installing Task Scheduler task..."; Flags: runhidden waituntilterminated; Check: IsTaskSchedulerChecked
; Launch the application (only if Task Scheduler was NOT set up, to avoid duplicate instances)
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Check: IsTaskSchedulerNotChecked

[UninstallRun]
; Kill running process before uninstalling
Filename: "taskkill.exe"; Parameters: "/F /IM DevanagariIME.exe /T"; Flags: runhidden waituntilterminated
; Remove Task Scheduler task during uninstall
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -NoProfile -File ""{app}\install-task-scheduler.ps1"" -Uninstall"; Flags: runhidden waituntilterminated

[Code]
var
  UninstallOnly: Boolean;

function IsDotNetInstalled: Boolean;
var
  Release: Cardinal;
  Success: Boolean;
  NetFrameworkPath: String;
  FindRec: TFindRec;
begin
  Result := False;
  
  // Method 1: Check registry for .NET Desktop Runtime (6.0, 7.0, or 8.0)
  // Check x64 sharedhost
  Success := RegQueryDWordValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', Release);
  if Success and (Release >= $60000) then
  begin
    Result := True;
    Exit;
  end;
  
  // Check WOW6432Node (for 32-bit on 64-bit systems)
  Success := RegQueryDWordValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', Release);
  if Success and (Release >= $60000) then
  begin
    Result := True;
    Exit;
  end;
  
  // Method 2: Check if .NET Desktop Runtime folder exists (most reliable)
  NetFrameworkPath := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if DirExists(NetFrameworkPath) then
  begin
    // Look for any version 6.0 or higher
    if FindFirst(NetFrameworkPath + '\6.*', FindRec) then
    begin
      Result := True;
      FindClose(FindRec);
      Exit;
    end;
    if FindFirst(NetFrameworkPath + '\7.*', FindRec) then
    begin
      Result := True;
      FindClose(FindRec);
      Exit;
    end;
    if FindFirst(NetFrameworkPath + '\8.*', FindRec) then
    begin
      Result := True;
      FindClose(FindRec);
      Exit;
    end;
  end;
  
  // Method 3: Check Program Files (x86) for 32-bit installs
  NetFrameworkPath := ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if DirExists(NetFrameworkPath) then
  begin
    if FindFirst(NetFrameworkPath + '\6.*', FindRec) then
    begin
      Result := True;
      FindClose(FindRec);
      Exit;
    end;
    if FindFirst(NetFrameworkPath + '\7.*', FindRec) then
    begin
      Result := True;
      FindClose(FindRec);
      Exit;
    end;
    if FindFirst(NetFrameworkPath + '\8.*', FindRec) then
    begin
      Result := True;
      FindClose(FindRec);
      Exit;
    end;
  end;
end;

function IsTaskSchedulerChecked: Boolean;
begin
  Result := WizardIsTaskSelected('startup');
end;

function IsTaskSchedulerNotChecked: Boolean;
begin
  Result := not WizardIsTaskSelected('startup');
end;

function KillProcess(ProcessName: String): Boolean;
var
  ResultCode: Integer;
begin
  // Kill the process using taskkill
  Result := Exec('taskkill', '/F /IM ' + ProcessName + ' /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  // It's okay if the process doesn't exist (ResultCode <> 0)
  Result := True;
end;

function InitializeSetup(): Boolean;
var
  UninstallString: String;
  ResultCode: Integer;
  MsgResult: Integer;
  AppPath: String;
  UninstallKey: String;
begin
  Result := True;
  UninstallOnly := False;
  
  // Check if already installed by looking for uninstaller in registry
  // Try multiple possible registry key formats
  UninstallKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\A1B2C3D4-E5F6-7890-ABCD-EF1234567890';
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE, UninstallKey, 'UninstallString', UninstallString) then
  begin
    // Try with braces (some Inno Setup versions)
    UninstallKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}';
    RegQueryStringValue(HKEY_LOCAL_MACHINE, UninstallKey, 'UninstallString', UninstallString);
  end;
  
  // Fallback: Check if installation directory exists with uninstaller
  if UninstallString = '' then
  begin
    AppPath := ExpandConstant('{autopf}\{#MyAppName}');
    if DirExists(AppPath) and FileExists(AppPath + '\unins000.exe') then
    begin
      UninstallString := AppPath + '\unins000.exe';
    end;
  end;
  
  // If we found an uninstaller, show the dialog
  if UninstallString <> '' then
  begin
    // App is already installed - show dialog with options
    MsgResult := MsgBox('Devanagari IME is already installed on your system.' + #13#10#13#10 +
      'What would you like to do?' + #13#10#13#10 +
      '• Click "Yes" to uninstall the existing version and then install the new version' + #13#10 +
      '• Click "No" to uninstall only (exit without installing)' + #13#10 +
      '• Click "Cancel" to exit without making any changes',
      mbConfirmation, MB_YESNOCANCEL);
    
    if MsgResult = IDYES then
    begin
      // Kill running process before uninstalling
      KillProcess('DevanagariIME.exe');
      Sleep(500);
      
      // Uninstall and reinstall (upgrade)
      UninstallString := RemoveQuotes(UninstallString);
      if Exec(UninstallString, '/SILENT', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        // Wait for uninstall to complete
        Sleep(2000);
        Result := True; // Continue with installation
      end
      else
      begin
        // If silent uninstall fails, try with minimal UI
        if Exec(UninstallString, '/VERYSILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
        begin
          Sleep(2000);
          Result := True; // Continue with installation
        end
        else
        begin
          MsgBox('Failed to uninstall existing version. Please uninstall manually from Control Panel first, then run this installer again.', mbError, MB_OK);
          Result := False;
        end;
      end;
    end
    else if MsgResult = IDNO then
    begin
      // Kill running process before uninstalling
      KillProcess('DevanagariIME.exe');
      Sleep(500);
      
      // Uninstall only
      UninstallString := RemoveQuotes(UninstallString);
      if Exec(UninstallString, '/SILENT', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        Sleep(2000);
        MsgBox('Devanagari IME has been uninstalled successfully.', mbInformation, MB_OK);
        UninstallOnly := True;
        Result := False; // Exit without installing
      end
      else
      begin
        // If silent uninstall fails, try with minimal UI
        if Exec(UninstallString, '/VERYSILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
        begin
          Sleep(2000);
          UninstallOnly := True;
          Result := False; // Exit without installing
        end
        else
        begin
          MsgBox('Failed to uninstall. Please uninstall manually from Control Panel.', mbError, MB_OK);
          Result := False;
        end;
      end;
    end
    else
    begin
      // IDCANCEL - exit without doing anything
      Result := False;
    end;
  end;
end;

