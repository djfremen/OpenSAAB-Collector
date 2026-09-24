$ErrorActionPreference='Stop'
$out=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force $out | Out-Null
$csc="$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
& $csc /nologo /target:winexe /platform:anycpu /optimize+ /out:"$out\OpenSAAB-Collector.exe" /win32manifest:"$PSScriptRoot\app.manifest" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Net.Http.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.ServiceProcess.dll "$PSScriptRoot\CaptureEngine.cs" "$PSScriptRoot\Collector.cs"
if($LASTEXITCODE -ne 0){throw 'Build failed'}
Get-FileHash "$out\OpenSAAB-Collector.exe" -Algorithm SHA256
