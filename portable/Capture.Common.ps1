# PowerShell 4-compatible helpers. No hardware or network calls when dot-sourced.
function Get-CaptureInterfaces([string[]]$Lines) {
    foreach ($line in $Lines) {
        if ($line -match '^interface \{value=(\\\\\.\\USBPcap[0-9]+)\}\{display=([^}]+)\}') {
            [pscustomobject]@{ Value=$Matches[1]; Label=$Matches[2] }
        }
    }
}
function Get-CaptureDevices([string[]]$Lines) {
    foreach ($line in $Lines) {
        if ($line -match '^value \{arg=[0-9]+\}\{value=([0-9]+)\}\{display=([^}]+)\}' -and $line -notmatch '\{parent=') {
            $number=[int]$Matches[1]
            if ($number -ge 1 -and $number -le 127) { [pscustomobject]@{ Address=$number; Label=$Matches[2] } }
        }
    }
}
function Get-CaptureArguments([string]$Interface,[int]$Address,[string]$Output) {
    if ($Interface -notmatch '^\\\\\.\\USBPcap[0-9]+$') { throw 'Invalid USBPcap interface' }
    if ($Address -lt 1 -or $Address -gt 127) { throw 'Invalid USB device address' }
    if ($Output -match '["\r\n]' -or -not $Output.EndsWith('.pcap')) { throw 'Invalid capture filename' }
    # No -A/new-device wildcard. Reconnect requires a fresh selection.
    return '-d "{0}" --devices {1} --inject-descriptors -s 65535 -o "{2}"' -f $Interface,$Address,$Output
}
function Get-CaptureSummary([string]$Path) {
    # USBPcapCMD writes classic pcap. Validate complete records, not just file existence.
    $f=[IO.File]::OpenRead($Path); $r=New-Object IO.BinaryReader($f)
    try {
        if ($f.Length -lt 24) { throw 'Capture has no complete pcap header' }
        if ($r.ReadUInt32() -ne [uint32]2712847316) { throw 'Expected little-endian classic pcap' }
        if ($r.ReadUInt16() -ne 2 -or $r.ReadUInt16() -ne 4) { throw 'Unsupported pcap version' }
        $f.Position=20
        if ($r.ReadUInt32() -ne 249) { throw 'Capture does not use USBPcap link type 249' }
        $packets=0; $truncated=0
        while ($f.Position -lt $f.Length) {
            if ($f.Length-$f.Position -lt 16) { throw 'Capture ended inside a packet header' }
            $null=$r.ReadUInt32();$null=$r.ReadUInt32()
            $included=$r.ReadUInt32();$original=$r.ReadUInt32()
            if ($included -gt $original -or $included -gt 65535 -or $included -gt $f.Length-$f.Position) { throw 'Capture contains an incomplete/invalid packet' }
            if ($included -lt $original) { $truncated++ }
            $f.Position += $included; $packets++
        }
        if ($packets -eq 0) { throw 'Capture contains no packets' }
        [pscustomobject]@{ Packets=$packets; TruncatedPackets=$truncated; Bytes=$f.Length; DiagnosticSuccess='not inferred' }
    } finally { $r.Dispose();$f.Dispose() }
}
function Invoke-CaptureQuery([string]$Executable,[string]$Interface='') {
    if ($Interface -and $Interface -notmatch '^\\\\\.\\USBPcap[0-9]+$') { throw 'Invalid USBPcap interface' }
    $p=New-Object Diagnostics.Process
    try {
        $p.StartInfo.FileName=$Executable
        $p.StartInfo.Arguments='--extcap-interfaces'
        if ($Interface) { $p.StartInfo.Arguments='--extcap-interface "'+$Interface+'" --extcap-config' }
        $p.StartInfo.UseShellExecute=$false
        $p.StartInfo.RedirectStandardOutput=$true
        $p.StartInfo.RedirectStandardError=$true
        $p.StartInfo.CreateNoWindow=$true
        $null=$p.Start()
        $out=$p.StandardOutput.ReadToEndAsync();$err=$p.StandardError.ReadToEndAsync()
        if (-not $p.WaitForExit(10000)) { $p.Kill();$p.WaitForExit();throw 'USBPcap device query timed out' }
        $output=$out.Result;$errors=$err.Result
        if ($p.ExitCode -ne 0) { throw ('USBPcap query failed: '+$errors) }
        $output -split '\r?\n'
    } finally { $p.Dispose() }
}
function Start-CaptureConsole([string]$Executable,[string]$Arguments) {
    # USBPcap is a GUI-subsystem executable and does not create its own console
    # when invoked with -d/-o. Give it an isolated console so q can stop it.
    $program=$Executable.Replace("'","''")
    $command=$Arguments.Replace("'","''")
    $script="`$p=Start-Process -FilePath '$program' -ArgumentList '$command' -NoNewWindow -PassThru; `$p.WaitForExit(); exit `$p.ExitCode"
    $encoded=[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
    Start-Process -FilePath "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -ArgumentList @('-NoProfile','-EncodedCommand',$encoded) -PassThru
}
