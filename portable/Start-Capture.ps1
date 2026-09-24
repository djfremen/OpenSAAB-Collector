#requires -version 4.0
[CmdletBinding()]
param([string]$UsbPcapPath='')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Capture.Common.ps1')
try {
    $identity=[Security.Principal.WindowsIdentity]::GetCurrent()
    $principal=New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Open Windows PowerShell as administrator, then run this script again.' }
    if (Get-Process USBPcapCMD -ErrorAction SilentlyContinue) { throw 'Another USBPcap capture is running. Stop it before starting this one.' }
    if (Get-Process emulator,Tech2Win -ErrorAction SilentlyContinue) { throw 'Close Tech2Win before starting, so initialization is included.' }
    if (-not $UsbPcapPath) {
        $candidates=@("$env:ProgramFiles\USBPcap\USBPcapCMD.exe","${env:ProgramFiles(x86)}\USBPcap\USBPcapCMD.exe","$env:ProgramFiles\Wireshark\extcap\USBPcapCMD.exe")
        $UsbPcapPath=$candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
    if (-not $UsbPcapPath -or -not (Test-Path -LiteralPath $UsbPcapPath)) { throw 'Install USBPcap from its official source and reboot first. No driver is installed by this helper.' }
    Write-Host 'OpenSAAB Collector - portable capture preview'
    Write-Host 'No DLL replacement, service, auto-upload or diagnostic commands.'
    Write-Host 'Captures can contain VIN, adapter serials and security traffic. Keep them private.'
    $adapter=Read-Host 'Adapter model and driver version (do not enter a VIN)'
    if ([string]::IsNullOrWhiteSpace($adapter)) { throw 'Adapter description is required' }
    $ifaces=@(Get-CaptureInterfaces @(& $UsbPcapPath --extcap-interfaces))
    if ($ifaces.Count -eq 0) { throw 'No USBPcap interfaces found. Check driver installation and reboot.' }
    for ($i=0;$i -lt $ifaces.Count;$i++) { Write-Host ('{0}: {1}' -f ($i+1),$ifaces[$i].Label) }
    $pick=Read-Host 'Root hub number (inspect devices next)'
    if ($pick -notmatch '^[0-9]+$' -or [int]$pick -lt 1 -or [int]$pick -gt $ifaces.Count) { throw 'Invalid hub selection' }
    $iface=$ifaces[[int]$pick-1].Value
    $devices=@(Get-CaptureDevices @(& $UsbPcapPath --extcap-interface $iface --extcap-config))
    foreach ($device in $devices) { Write-Host ('{0}: {1}' -f $device.Address,$device.Label) }
    $address=Read-Host 'Exact adapter USB address from this list (not VID/PID)'
    if ($address -notmatch '^[0-9]+$') { throw 'Invalid device address' }
    $selected=@($devices | Where-Object { $_.Address -eq [int]$address })
    if ($selected.Count -ne 1) { throw 'Select one listed device; restart to inspect a different hub' }
    Write-Host ('Will capture ONLY address {0} on {1}: {2}' -f $address,$iface,$selected[0].Label)
    Write-Host 'Keep adapter connected. If it reconnects, stop and select its new address.'
    if ((Read-Host 'Type CAPTURE to begin this local recording') -cne 'CAPTURE') { return }
    $base=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'OpenSAAB-Captures'
    $session=Join-Path $base ((Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
    $null=New-Item -ItemType Directory -Path $session -Force
    $capture=Join-Path $session 'usb.pcap';$events=Join-Path $session 'actions.jsonl'
    $manifest=[ordered]@{ version='portable-preview-0.1'; adapter=$adapter; usb_interface=$iface; usb_address=[int]$address; device_label=$selected[0].Label; started_utc=[DateTime]::UtcNow.ToString('o'); os=[Environment]::OSVersion.VersionString; powershell=$PSVersionTable.PSVersion.ToString(); capture_state='starting'; uploaded=$false; diagnostic_success='not inferred' }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $session 'session.json') -Encoding UTF8
    $argsLine=Get-CaptureArguments $iface ([int]$address) $capture
    $proc=Start-Process -FilePath $UsbPcapPath -ArgumentList $argsLine -PassThru
    Start-Sleep -Seconds 2
    if ($proc.HasExited) { throw ('USBPcap exited early. Keep the session folder for troubleshooting: '+$session) }
    Write-Host 'Use the USBPcap window to confirm recording has started BEFORE launching Tech2Win.'
    Write-Host 'In this window, enter action notes immediately before each menu action.'
    Write-Host 'Suggested: launch Tech2Win, select Mongoose driver, read VIN, ECM information, read DTC.'
    Write-Host 'Do not clear codes, program modules or request security access for this first capture.'
    Write-Host 'To finish: press Ctrl+C in the USBPcap window ONLY, then type DONE here.'
    while ($true) {
        $note=Read-Host 'Action note / DONE'
        if ($note -ceq 'DONE') {
            if (-not $proc.HasExited) { Write-Host 'USBPcap is still running. Stop it in its own window first.';continue }
            break
        }
        if ($note) { [ordered]@{utc=[DateTime]::UtcNow.ToString('o');action=$note} | ConvertTo-Json -Compress | Add-Content -LiteralPath $events -Encoding UTF8 }
    }
    $manifest['stopped_utc']=[DateTime]::UtcNow.ToString('o')
    $manifest['capture_state']='stopped'
    $manifest['process_exit_code']=$proc.ExitCode
    try {
        $summary=Get-CaptureSummary $capture
        $manifest['capture_summary']=$summary
        $manifest['sha256']=(Get-FileHash -LiteralPath $capture -Algorithm SHA256).Hash.ToLowerInvariant()
        $manifest['validation']='structurally valid; adapter exchanges still require review'
        Write-Host ('Saved {0} packets ({1} truncated). This does not prove diagnostic success.' -f $summary.Packets,$summary.TruncatedPackets)
    } catch { $manifest['validation']='failed: '+$_.Exception.Message;Write-Warning $manifest['validation'] }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $session 'session.json') -Encoding UTF8
    Write-Host ('Files saved locally: '+$session)
    Write-Host 'Review in Wireshark before privately sharing. No files have been uploaded.'
} catch { Write-Error $_; exit 1 }
