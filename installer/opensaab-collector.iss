; OpenSAAB Collector — InnoSetup 6 installer script.
;
; Build: iscc opensaab-collector.iss
; Output: Output\opensaab-collector-setup-0.1.0.exe
;
; Pre-requisites for build (see ../README.md for the full pipeline):
;   - Service published:  src\OpenSAAB.Collector.Service\bin\Release\net8.0\win-x64\publish\OpenSAAB.Collector.Service.exe
;   - Tray published:     src\OpenSAAB.Collector.Tray\bin\Release\net8.0-windows\win-x64\publish\OpenSAAB.Collector.Tray.exe
;   - Shim DLLs built:    ..\..\Chipsoft_RE\shim\cstech2win\build\CSTech2Win.dll
;                         ..\..\Chipsoft_RE\shim\j2534\build\j2534_interface.dll

#define AppName        "OpenSAAB Collector"
#define AppVersion     "0.2.7"
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
; Shim DLLs go straight into the Chipsoft install dir.
; replacesameversion + uninsneveruninstall: at uninstall the [UninstallRun]
; section will instead restore the *_real.dll backups, so we don't want
; the uninstaller to also delete our shim — it'd race the restore.
Source: "..\..\Chipsoft_RE\shim\cstech2win\build\CSTech2Win.dll"; \
    DestDir: "{#ChipsoftDir}"; DestName: "CSTech2Win.dll"; \
    Flags: ignoreversion uninsneveruninstall

; v0.1.7: j2534_interface.dll shim removed from default install. Tech2Win
; uses CSTech2Win.dll (D-PDU API), not j2534_interface.dll, so the j2534
; shim sat idle for the median contributor — only TrionicCANFlasher /
; OpenPort users would benefit. Strip-down keeps the install lean and
; avoids one more Restart Manager corner case. Source still lives in
; Chipsoft_RE/shim/j2534/ for advanced users who want to swap manually.

; Service + tray go to {app} (Program Files\OpenSAAB\Collector).
Source: "..\src\OpenSAAB.Collector.Service\bin\Release\net8.0\win-x64\publish\OpenSAAB.Collector.Service.exe"; \
    DestDir: "{app}"; Flags: ignoreversion

Source: "..\src\OpenSAAB.Collector.Tray\bin\Release\net8.0-windows\win-x64\publish\OpenSAAB.Collector.Tray.exe"; \
    DestDir: "{app}"; Flags: ignoreversion

Source: "consent.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\OpenSAAB Collector Tray"; Filename: "{app}\OpenSAAB.Collector.Tray.exe"
Name: "{group}\Uninstall OpenSAAB Collector"; Filename: "{uninstallexe}"; Comment: "Stops the service, restores the genuine Chipsoft DLLs, removes everything"
Name: "{userstartup}\OpenSAAB Collector Tray"; Filename: "{app}\OpenSAAB.Collector.Tray.exe"

[Tasks]
Name: "consentupload"; Description: "Upload captured logs to openSAAB.com (recommended for community contributors)"; GroupDescription: "Data sharing:"; Flags: unchecked

[Registry]
; ConsentVersion is set whichever way the user goes — even local-only
; users acknowledged the disclosure.
Root: HKLM; Subkey: "SOFTWARE\OpenSAAB\Collector"; ValueType: string; ValueName: "ConsentVersion"; ValueData: "v1"; Flags: uninsdeletekey
; openSAAB.com DNS hasn't been pointed at the Koyeb deployment yet — until
; that lands, ship the Koyeb domain directly so fresh installs don't 404.
Root: HKLM; Subkey: "SOFTWARE\OpenSAAB\Collector"; ValueType: string; ValueName: "IngestUrl"; ValueData: "https://relevant-diann-djfremen2-c013cdc3.koyeb.app/ingest/shim-log"
; UploadEnabled mirrors the consentupload task — written via [Code] below.
; UploadCount: pre-create with users-modify so the unelevated tray can
; increment it after each successful upload (v0.1.7 fix; previously the
; tray's IncrementUploadCount silently failed on HKLM permission denied).
Root: HKLM; Subkey: "SOFTWARE\OpenSAAB\Collector"; ValueType: dword; ValueName: "UploadCount"; ValueData: "0"; Permissions: users-modify; Flags: uninsdeletevalue createvalueifdoesntexist

[Run]
; --- Pre-install: refuse if Chipsoft isn't there. Done in [Code] PrepareToInstall. ---

; Backup the genuine Chipsoft DLLs (only on first install — guarded by
; "exist" check so a re-install doesn't clobber an existing backup).
Filename: "{cmd}"; \
    Parameters: "/c if not exist ""{#ChipsoftDir}\CSTech2Win_real.dll"" move /Y ""{#ChipsoftDir}\CSTech2Win.dll"" ""{#ChipsoftDir}\CSTech2Win_real.dll"""; \
    StatusMsg: "Backing up genuine CSTech2Win.dll…"; \
    Flags: runhidden waituntilterminated; \
    BeforeInstall: NoOp

; v0.1.7: j2534 backup step removed alongside the shim drop.

; Install + start the Windows Service.
Filename: "{sys}\sc.exe"; Parameters: "create {#ServiceName} binPath= ""\""{app}\OpenSAAB.Collector.Service.exe\"""" start= auto DisplayName= ""OpenSAAB Collector"""; \
    StatusMsg: "Installing OpenSAAB Collector service…"; \
    Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "description {#ServiceName} ""Watches %TEMP% for shim logs from Tech2Win / J2534 clients and uploads to openSAAB.com if consent given."""; \
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

; Restore genuine Chipsoft DLLs.
Filename: "{cmd}"; \
    Parameters: "/c if exist ""{#ChipsoftDir}\CSTech2Win_real.dll"" (del /Q ""{#ChipsoftDir}\CSTech2Win.dll"" & move /Y ""{#ChipsoftDir}\CSTech2Win_real.dll"" ""{#ChipsoftDir}\CSTech2Win.dll"")"; \
    Flags: runhidden waituntilterminated
; v0.1.7: also restore the genuine j2534_interface.dll if a previous Collector
; (≤ v0.1.6) had backed it up — keeps uninstall idempotent across versions.
Filename: "{cmd}"; \
    Parameters: "/c if exist ""{#ChipsoftDir}\j2534_interface_real.dll"" (del /Q ""{#ChipsoftDir}\j2534_interface.dll"" & move /Y ""{#ChipsoftDir}\j2534_interface_real.dll"" ""{#ChipsoftDir}\j2534_interface.dll"")"; \
    Flags: runhidden waituntilterminated

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ChipsoftCs, ChipsoftJ: String;
  RC: Integer;
begin
  Result := '';
  ChipsoftCs := ExpandConstant('{#ChipsoftDir}\CSTech2Win.dll');
  ChipsoftJ  := ExpandConstant('{#ChipsoftDir}\j2534_interface.dll');
  if (not FileExists(ChipsoftCs)) and (not FileExists(ExpandConstant('{#ChipsoftDir}\CSTech2Win_real.dll'))) then begin
    Result := 'CSTech2Win.dll not found in ' + ExpandConstant('{#ChipsoftDir}') + #13#10 +
              'OpenSAAB Collector requires a working Chipsoft J2534 Pro install. Please install Chipsoft first, then re-run this installer.';
    Exit;
  end;
  if (not FileExists(ChipsoftJ)) and (not FileExists(ExpandConstant('{#ChipsoftDir}\j2534_interface_real.dll'))) then begin
    Result := 'j2534_interface.dll not found in ' + ExpandConstant('{#ChipsoftDir}') + #13#10 +
              'OpenSAAB Collector requires a working Chipsoft J2534 Pro install.';
    Exit;
  end;
  // Stop the previous Collector so its tray.exe and service.exe stop
  // holding their own files open. Restart Manager often misses the
  // unelevated tray, leaving the user staring at "Setup was unable to
  // automatically close all applications" mid-upgrade.
  Exec(ExpandConstant('{cmd}'),
       '/c sc stop {#ServiceName} >nul 2>&1 & taskkill /F /IM OpenSAAB.Collector.Tray.exe >nul 2>&1',
       '', SW_HIDE, ewWaitUntilTerminated, RC);
  // v0.1.7: Tech2Win's diagnostic engine (emulator.exe) often stays running
  // headless after the GUI window closes, holding our shim DLL open. Restart
  // Manager's WM_CLOSE doesn't reach a windowless process, so installs hang on
  // "files in use." Kill ONLY orphaned (no MainWindowTitle) emulator.exe
  // instances; leave alone any with a visible window so an active Tech2Win
  // session still gets the polite Restart Manager prompt.
  Exec(ExpandConstant('{cmd}'),
       '/c powershell -NoProfile -Command "Get-Process -Name emulator -ErrorAction SilentlyContinue | Where-Object { [string]::IsNullOrEmpty($_.MainWindowTitle) } | Stop-Process -Force -ErrorAction SilentlyContinue"',
       '', SW_HIDE, ewWaitUntilTerminated, RC);
end;

procedure NoOp;
begin
  // [Run] BeforeInstall placeholder.
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
