$ErrorActionPreference='Stop'
& "$PSScriptRoot\build.ps1"
$out=Join-Path $PSScriptRoot 'bin'
$csc="$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
& $csc /nologo /target:exe /out:"$out\CaptureIdentityTest.exe" /r:"$out\OpenSAAB-Collector.exe" "$PSScriptRoot\CaptureIdentityTest.cs"
if($LASTEXITCODE -ne 0){throw 'Capture identity tests failed to compile'}
& "$out\CaptureIdentityTest.exe"
if($LASTEXITCODE -ne 0){throw 'Capture identity tests failed'}
& $csc /nologo /target:exe /out:"$out\UsbDevicesTest.exe" /r:"$out\OpenSAAB-Collector.exe" "$PSScriptRoot\UsbDevicesTest.cs"
if($LASTEXITCODE -ne 0){throw 'USB device picker tests failed to compile'}
& "$out\UsbDevicesTest.exe"
if($LASTEXITCODE -ne 0){throw 'USB device picker tests failed'}
& $csc /nologo /target:exe /out:"$out\BundleTest.exe" /r:"$out\OpenSAAB-Collector.exe" /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll "$PSScriptRoot\BundleTest.cs"
if($LASTEXITCODE -ne 0){throw 'Bundle tests failed to compile'}
& "$out\BundleTest.exe"
if($LASTEXITCODE -ne 0){throw 'Bundle tests failed'}
& $csc /nologo /target:exe /out:"$out\SanitizeTest.exe" /r:"$out\OpenSAAB-Collector.exe" /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll "$PSScriptRoot\SanitizeTest.cs"
if($LASTEXITCODE -ne 0){throw 'Sanitizer tests failed to compile'}
& "$out\SanitizeTest.exe"
if($LASTEXITCODE -ne 0){throw 'Sanitizer tests failed'}
& $csc /nologo /target:exe /out:"$out\CollectorLayoutTest.exe" /r:"$out\OpenSAAB-Collector.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll "$PSScriptRoot\CollectorLayoutTest.cs"
if($LASTEXITCODE -ne 0){throw 'Collector layout tests failed to compile'}
& "$out\CollectorLayoutTest.exe"
if($LASTEXITCODE -ne 0){throw 'Collector layout tests failed'}
