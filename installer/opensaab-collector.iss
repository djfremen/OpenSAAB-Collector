; OpenSAAB Collector — InnoSetup 6 installer script.
;
; Build: iscc opensaab-collector.iss
; Output: Output\opensaab-collector-setup-0.4.0.exe
;
; v0.4.0: the DLL-shim model is retired. The Collector no longer copies any
; file into the Chipsoft folder. It installs a Windows Service + tray app;
; the service switches on the genuine Chipsoft driver's own logging by
; setting LogLevel:0 in C:\ProgramData\CHIPSOFT_J2534\options.json and
; harvests the driver's *.log files. Nothing to back up, nothing to restore.
;
; Pre-requisites for build (see ../README.md for the full pipeline):
;   - Service published:  src\OpenSAAB.Collector.Service\bin\Release\net8.0\win-x64\publish\OpenSAAB.Collector.Service.exe
;   - Tray published:     src\OpenSAAB.Collector.Tray\bin\Release\net8.0-windows\win-x64\publish\OpenSAAB.Collector.Tray.exe

#define AppName        "OpenSAAB Collector"
#define AppVersion     "0.4.1"
#define AppPublisher   "OpenSAAB"
#define AppURL         "https://opensaab.com"
#define ServiceName    "OpenSAABCollector"
#define ChipsoftDir    "{commonpf32}\CHIPSOFT_J2534_Pro_Driver"

[Setup]
AppId={{8A2F9C00-OPEN-SAAB-0001-COLLECTOR0001}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
DefaultDirName={autopf}\OpenSAAB\Collector
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
OutputBaseFilename=opensaab-collector-setup-{#AppVersion}
OutputDir=Output
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
LicenseFile=..\LICENSE
InfoBeforeFile=consent.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; v0.4.0: NO shim DLLs. Only the service + tray go to {app}.
Source: "..\src\OpenSAAB.Collector.Service\bin\Release\net8.0\win-x64\publish\OpenSAAB.Collector.Service.exe"; \
    DestDir: "{app}"; Flags: ignoreversion

Source: "..\src\OpenSAAB.Collector.Tray\bin\Release\net8.0-windows\win-x64\publish\OpenSAAB.Collector.Tray.exe"; \
    DestDir: "{app}"; Flags: ignoreversion

Source: "consent.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\OpenSAAB Collector Tray"; Filename: "{app}\OpenSAAB.Collector.Tray.exe"
Name: "{group}\Uninstall OpenSAAB Collector"; Filename: "{uninstallexe}"; Comment: "Stops the service and removes everything"
Name: "{userstartup}\OpenSAAB Collector Tray"; Filename: "{app}\OpenSAAB.Collector.Tray.exe"

[Tasks]
Name: "consentupload"; Description: "Upload captured logs to openSAAB.com (recommended for community contributors)"; GroupDescription: "Data sharing:"; Flags: unchecked

[Registry]
; ConsentVersion is set whichever way the user goes — even local-only
; users acknowledged the disclosure. Bumped to v2 for the v0.4.0 model
; change (driver-native logging instead of DLL shims).
Root: HKLM; Subkey: "SOFTWARE\OpenSAAB\Collector"; ValueType: string; ValueName: "ConsentVersion"; ValueData: "v2"; Flags: uninsdeletekey
; openSAAB.com DNS hasn't been pointed at the Koyeb deployment yet — until
; that lands, ship the Koyeb domain directly so fresh installs don't 404.
Root: HKLM; Subkey: "SOFTWARE\OpenSAAB\Collector"; ValueType: string; ValueName: "IngestUrl"; ValueData: "https://relevant-diann-djfremen2-c013cdc3.koyeb.app/ingest/shim-log"
; UploadCount: pre-create with users-modify so the unelevated tray can
; increment it after each successful upload.
Root: HKLM; Subkey: "SOFTWARE\OpenSAAB\Collector"; ValueType: dword; ValueName: "UploadCount"; ValueData: "0"; Permissions: users-modify; Flags: uninsdeletevalue createvalueifdoesntexist

; v0.4.1: NO [Dirs] entry for the Chipsoft logs subdirectory. That folder
; belongs to the OEM driver — it creates it lazily when its Boost.Log
; sink first writes. Pre-creating it can mask diagnostic signal and may
; interfere with the driver's own sink-init path. The Service polls and
; lazily attaches its FileSystemWatcher once the driver creates the dir.

[Run]
; --- Pre-install: refuse if Chipsoft isn't there. Done in [Code] PrepareToInstall. ---

; Migration: if a pre-v0.4.0 Collector swapped in DLL shims, restore the
; genuine Chipsoft DLLs from the *_real.dll backups so the retired shims
; stop running. Harmless if no backup exists (fresh install).
Filename: "{cmd}"; \
    Parameters: "/c if exist ""{#ChipsoftDir}\CSTech2Win_real.dll"" (del /Q ""{#ChipsoftDir}\CSTech2Win.dll"" & move /Y ""{#ChipsoftDir}\CSTech2Win_real.dll"" ""{#ChipsoftDir}\CSTech2Win.dll"")"; \
    StatusMsg: "Restoring genuine Chipsoft DLL (migration from shim era)…"; \
    Flags: runhidden waituntilterminated
Filename: "{cmd}"; \
    Parameters: "/c if exist ""{#ChipsoftDir}\j2534_interface_real.dll"" (del /Q ""{#ChipsoftDir}\j2534_interface.dll"" & move /Y ""{#ChipsoftDir}\j2534_interface_real.dll"" ""{#ChipsoftDir}\j2534_interface.dll"")"; \
    Flags: runhidden waituntilterminated

; Install + start the Windows Service.
Filename: "{sys}\sc.exe"; Parameters: "create {#ServiceName} binPath= ""\""{app}\OpenSAAB.Collector.Service.exe\"""" start= auto DisplayName= ""OpenSAAB Collector"""; \
    StatusMsg: "Installing OpenSAAB Collector service…"; \
    Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "description {#ServiceName} ""Switches on the Chipsoft J2534 driver's native logging and uploads the resulting logs to openSAAB.com if consent is given."""; \
    Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "start {#ServiceName}"; \
    StatusMsg: "Starting OpenSAAB Collector service…"; \
    Flags: runhidden waituntilterminated

; Launch the tray app immediately.
Filename: "{app}\OpenSAAB.Collector.Tray.exe"; \
    Description: "Launch tray app"; \
    Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop + delete service.
Filename: "{sys}\sc.exe"; Parameters: "stop {#ServiceName}"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden waituntilterminated
; Kill the tray app if it's running.
Filename: "{cmd}"; Parameters: "/c taskkill /IM OpenSAAB.Collector.Tray.exe /F >nul 2>&1"; Flags: runhidden waituntilterminated
; v0.4.0 never modified the Chipsoft folder, so there is nothing to restore.

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ChipsoftDirPath, ChipsoftCs: String;
  RC: Integer;
begin
  Result := '';
  ChipsoftDirPath := ExpandConstant('{#ChipsoftDir}');
  ChipsoftCs := ChipsoftDirPath + '\CSTech2Win.dll';
  // Require a working Chipsoft J2534 Pro install. Accept either the live
  // DLL or a *_real.dll backup left by a pre-v0.4.0 Collector.
  if (not DirExists(ChipsoftDirPath)) or
     ((not FileExists(ChipsoftCs)) and
      (not FileExists(ChipsoftDirPath + '\CSTech2Win_real.dll'))) then begin
    Result := 'Chipsoft J2534 Pro driver not found in ' + ChipsoftDirPath + #13#10 +
              'OpenSAAB Collector requires a working Chipsoft J2534 Pro install. ' +
              'Please install Chipsoft first, then re-run this installer.';
    Exit;
  end;
  // Stop the previous Collector so its tray.exe and service.exe stop
  // holding their own files open.
  Exec(ExpandConstant('{cmd}'),
       '/c sc stop {#ServiceName} >nul 2>&1 & taskkill /F /IM OpenSAAB.Collector.Tray.exe >nul 2>&1',
       '', SW_HIDE, ewWaitUntilTerminated, RC);
  // Migration aid: a pre-v0.4.0 install left DLL shims in the Chipsoft
  // folder. Tech2Win's headless emulator.exe can keep a shim DLL loaded
  // after the GUI closes, blocking the [Run] restore step. Kill ONLY
  // orphaned (no MainWindowTitle) emulator.exe instances so the genuine
  // DLL can be moved back into place; leave any with a visible window.
  Exec(ExpandConstant('{cmd}'),
       '/c powershell -NoProfile -Command "Get-Process -Name emulator -ErrorAction SilentlyContinue | Where-Object { [string]::IsNullOrEmpty($_.MainWindowTitle) } | Stop-Process -Force -ErrorAction SilentlyContinue"',
       '', SW_HIDE, ewWaitUntilTerminated, RC);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  EnableUpload: Cardinal;
begin
  if CurStep = ssPostInstall then begin
    if IsTaskSelected('consentupload') then EnableUpload := 1 else EnableUpload := 0;
    RegWriteDWordValue(HKLM, 'SOFTWARE\OpenSAAB\Collector', 'UploadEnabled', EnableUpload);
  end;
end;
