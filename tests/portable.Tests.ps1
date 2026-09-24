$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../portable/Capture.Common.ps1')
$checks=0
function Assert($Value,[string]$Message) { if (-not $Value) { throw $Message }; $script:checks++ }
function Reject([scriptblock]$Block,[string]$Message) { $failed=$false;try { & $Block | Out-Null } catch { $failed=$true }; Assert $failed $Message }
$interfaces=@(Get-CaptureInterfaces @('interface {value=\\.\USBPcap2}{display=USBPcap2}','interface {value=bogus}{display=Bad}'))
Assert ($interfaces.Count -eq 1 -and $interfaces[0].Value -eq '\\.\USBPcap2') 'Interfaces parsed incorrectly'
$devices=@(Get-CaptureDevices @('value {arg=99}{value=21}{display=[21] Mongoose}{enabled=true}','value {arg=99}{value=21_1}{display=child}{parent=21}','value {arg=99}{value=22}{display=child}{parent=21}','value {arg=99}{value=999}{display=invalid}'))
Assert ($devices.Count -eq 1 -and $devices[0].Address -eq 21) 'Device selection accepted child/invalid address'
$line=Get-CaptureArguments '\\.\USBPcap2' 21 'C:\Users\Test User\usb.pcap'
Assert ($line -eq '-d "\\.\USBPcap2" --devices 21 --inject-descriptors -s 65535 -o "C:\Users\Test User\usb.pcap"') 'Capture arguments differ'
Reject { Get-CaptureArguments 'bad' 21 'test.pcap' } 'Invalid interface accepted'
Reject { Get-CaptureArguments '\\.\USBPcap1' 0 'test.pcap' } 'Address zero accepted'
Reject { Get-CaptureArguments '\\.\USBPcap1' 128 'test.pcap' } 'Invalid address accepted'
Reject { Get-CaptureArguments '\\.\USBPcap1' 1 'bad"name.pcap' } 'Quote accepted in path'
$root=Join-Path ([IO.Path]::GetTempPath()) ('collector-test-'+[Guid]::NewGuid().ToString('N'))
$null=New-Item -ItemType Directory $root
function Write-Fixture([string]$Path,[uint32]$Link=249,[int]$Mode=1) {
 $f=[IO.File]::Create($Path);$w=New-Object IO.BinaryWriter($f)
 try {
  $w.Write([uint32]2712847316);$w.Write([uint16]2);$w.Write([uint16]4);$w.Write([int]0);$w.Write([uint32]0);$w.Write([uint32]65535);$w.Write($Link)
  if ($Mode -gt 0) { $w.Write([uint32]1);$w.Write([uint32]0);$w.Write([uint32]27);$w.Write([uint32]$(if($Mode -eq 2){50}else{27}));$w.Write((New-Object byte[] 27)) }
 } finally {$w.Dispose();$f.Dispose()}
}
try {
 $p=Join-Path $root 'test.pcap'; Write-Fixture $p
 $s=Get-CaptureSummary $p
 Assert ($s.Packets -eq 1 -and $s.TruncatedPackets -eq 0 -and $s.DiagnosticSuccess -eq 'not inferred') 'Good pcap summary failed'
 Write-Fixture $p 249 2
 Assert ((Get-CaptureSummary $p).TruncatedPackets -eq 1) 'Truncated payload not detected'
 Write-Fixture $p 249 0
 Reject { Get-CaptureSummary $p } 'Empty capture accepted'
 Write-Fixture $p 1 1
 Reject { Get-CaptureSummary $p } 'Wrong link type accepted'
 Write-Fixture $p
 $bytes=[IO.File]::ReadAllBytes($p);[IO.File]::WriteAllBytes($p,$bytes[0..($bytes.Length-2)])
 Reject { Get-CaptureSummary $p } 'Partial packet accepted'
 [IO.File]::WriteAllBytes($p,(New-Object byte[] 7))
 Reject { Get-CaptureSummary $p } 'Short header accepted'
 foreach($file in (Get-ChildItem (Join-Path $PSScriptRoot '../portable') -Filter '*.ps1')) {
  $tokens=$null;$errors=$null;$null=[Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors)
  Assert ($errors.Count -eq 0) ('Parse errors in '+$file.Name+': '+($errors|Out-String))
 }
 Write-Host "PASS: $checks portable helper checks (no real USB or Windows compatibility claim)"
} finally { Remove-Item -LiteralPath $root -Recurse -Force }
