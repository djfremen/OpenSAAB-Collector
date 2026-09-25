$ErrorActionPreference='Stop'
& "$PSScriptRoot\build.ps1"
$out=Join-Path $PSScriptRoot 'bin'
$csc="$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
& $csc /nologo /target:exe /out:"$out\BundleTest.exe" /r:"$out\OpenSAAB-Collector.exe" /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll "$PSScriptRoot\BundleTest.cs"
if($LASTEXITCODE -ne 0){throw 'Bundle tests failed to compile'}
& "$out\BundleTest.exe"
if($LASTEXITCODE -ne 0){throw 'Bundle tests failed'}
& $csc /nologo /target:exe /out:"$out\SanitizeTest.exe" /r:"$out\OpenSAAB-Collector.exe" /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll "$PSScriptRoot\SanitizeTest.cs"
if($LASTEXITCODE -ne 0){throw 'Sanitizer tests failed to compile'}
& "$out\SanitizeTest.exe"
if($LASTEXITCODE -ne 0){throw 'Sanitizer tests failed'}
