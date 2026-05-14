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

    Write-Host '== Building bundled scapy decoder =='
    # Optional -- if Python isn't on PATH we skip with a warning. Installer
    # has skipifsourcedoesntexist on the decoder Source line.
    if ((Get-Command python -ErrorAction SilentlyContinue) -or
        (Get-Command py     -ErrorAction SilentlyContinue)) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File installer/decoder/build-decoder.ps1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "decoder build failed; installer will ship without fremsoft-decoder.exe"
        }
    } else {
        Write-Warning "python not on PATH; skipping decoder build (installer will ship without fremsoft-decoder.exe)"
    }

    Write-Host '== Building installer =='
    # winget installs Inno Setup 6 per-user under $LOCALAPPDATA, not on PATH.
    # Look for iscc in the standard places before bailing.
    $isccCandidates = @(
        (Get-Command iscc.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source),
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\iscc.exe",
        "$env:ProgramFiles\Inno Setup 6\iscc.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe"
    ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    if (-not $isccCandidates) {
        throw 'iscc.exe not found. Install Inno Setup 6 (winget install JRSoftware.InnoSetup) and re-run.'
    }
    Write-Host "  using iscc: $isccCandidates"
    & $isccCandidates installer/opensaab-collector.iss
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
