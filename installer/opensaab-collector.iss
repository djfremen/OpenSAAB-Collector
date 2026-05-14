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
#define AppVersion     "0.1.1"
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

Source: "..\..\Chipsoft_RE\shim\j2534\build\j2534_interface.dll"; \
    DestDir: "{#ChipsoftDir}"; DestName: "j2534_interface.dll"; \
    Flags: ignoreversion uninsneveruninstall

; Service + tray go to {app} (Program Files\OpenSAAB\Collector).
Source: "..\src\OpenSAAB.Collector.Service\bin\Release\net8.0\win-x64\publish\OpenSAAB.Collector.Service.exe"; \
    DestDir: "{app}"; Flags: ignoreversion

Source: "..\src\OpenSAAB.Collector.Tray\bin\Release\net8.0-windows\win-x64\publish\OpenSAAB.Collector.Tray.exe"; \
    DestDir: "{app}"; Flags: ignoreversion

Source: "consent.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\OpenSAAB Collector Tray"; Filename: "{app}\OpenSAAB.Collector.Tray.exe"
Name: "{userstartup}\OpenSAAB Collector Tray"; Filename: "{app}\OpenSAAB.Collector.Tray.exe"

[Tasks]
Name: "consentupload"; Description: "Upload captured logs to openSAAB.com (recommended for community contributors)"; GroupDescription: "Data sharing:"; Flags: unchecked

[Registry]
; ConsentVersion is set whichever way the user goes — even local-only
; users acknowledged the disclosure.
Root: HKLM; Subkey: "SOFTWARE\OpenSAAB\Collector"; ValueType: string; ValueName: "ConsentVersion"; ValueData: "v1"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\OpenSAAB\Collector"; ValueType: string; ValueName: "IngestUrl"; ValueData: "https://openSAAB.com/ingest/shim-log"
; UploadEnabled mirrors the consentupload task — written via [Code] below.

[Run]
; --- Pre-install: refuse if Chipsoft isn't there. Done in [Code] PrepareToInstall. ---

; Backup the genuine Chipsoft DLLs (only on first install — guarded by
; "exist" check so a re-install doesn't clobber an existing backup).
Filename: "{cmd}"; \
    Parameters: "/c if not exist ""{#ChipsoftDir}\CSTech2Win_real.dll"" move /Y ""{#ChipsoftDir}\CSTech2Win.dll"" ""{#ChipsoftDir}\CSTech2Win_real.dll"""; \
    StatusMsg: "Backing up genuine CSTech2Win.dll…"; \
    Flags: runhidden waituntilterminated; \
    BeforeInstall: NoOp

Filename: "{cmd}"; \
    Parameters: "/c if not exist ""{#ChipsoftDir}\j2534_interface_real.dll"" move /Y ""{#ChipsoftDir}\j2534_interface.dll"" ""{#ChipsoftDir}\j2534_interface_real.dll"""; \
    StatusMsg: "Backing up genuine j2534_interface.dll…"; \
    Flags: runhidden waituntilterminated

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
Filename: "{cmd}"; \
    Parameters: "/c if exist ""{#ChipsoftDir}\j2534_interface_real.dll"" (del /Q ""{#ChipsoftDir}\j2534_interface.dll"" & move /Y ""{#ChipsoftDir}\j2534_interface_real.dll"" ""{#ChipsoftDir}\j2534_interface.dll"")"; \
    Flags: runhidden waituntilterminated

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ChipsoftCs, ChipsoftJ: String;
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
