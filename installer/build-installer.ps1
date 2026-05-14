# Build OpenSAAB Collector installer end-to-end.
# Run from the repo root: pwsh installer\build-installer.ps1

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Split-Path -Parent $ScriptDir

Push-Location $RepoRoot
try {
    Write-Host '== Restoring + publishing Service =='
    dotnet publish src/OpenSAAB.Collector.Service/OpenSAAB.Collector.Service.csproj `
        -c Release -r win-x64 --self-contained `
        -p:PublishSingleFile=true `
        -p:DebugType=embedded
    if ($LASTEXITCODE -ne 0) { throw 'Service publish failed' }

    Write-Host '== Restoring + publishing Tray =='
    dotnet publish src/OpenSAAB.Collector.Tray/OpenSAAB.Collector.Tray.csproj `
        -c Release -r win-x64 --self-contained `
        -p:PublishSingleFile=true `
        -p:DebugType=embedded
    if ($LASTEXITCODE -ne 0) { throw 'Tray publish failed' }

    Write-Host '== Building installer =='
    $iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if (-not $iscc) {
        throw 'iscc.exe not on PATH. Install Inno Setup 6 from https://jrsoftware.org/isinfo.php and re-run.'
    }
    & iscc.exe installer/opensaab-collector.iss
    if ($LASTEXITCODE -ne 0) { throw 'iscc failed' }

    $output = Get-ChildItem installer/Output/opensaab-collector-setup-*.exe `
        | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host ''
    Write-Host "== Installer ready: $($output.FullName) =="
    Write-Host "   Size: $([math]::Round($output.Length / 1MB, 2)) MB"
}
finally {
    Pop-Location
}
