# Build fremsoft-decoder.exe -- PyInstaller-bundled scapy decoder.
#
# Wraps Chipsoft_RE/tools/shim_log_decode.py into a single self-contained
# Windows exe. Result: one binary, no Python install needed on the target
# machine, can read live tail from stdin via the `-` arg.
#
# Output: installer\decoder\Output\fremsoft-decoder.exe
#
# Run on the EliteBook (or any Windows host with Python 3 on PATH).

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)

# Source decoder lives in the Chipsoft_RE repo, which is a sibling clone.
$DecoderSrc = Join-Path (Split-Path -Parent $RepoRoot) 'Chipsoft_RE\tools\shim_log_decode.py'
if (-not (Test-Path $DecoderSrc)) {
    throw "Decoder source not found at $DecoderSrc -- clone Chipsoft_RE alongside OpenSAAB-Collector."
}

Push-Location $ScriptDir
try {
    # Prefer the Windows 'py' launcher (canonical, avoids the MS Store
    # 'python' shim that appears on PATH but isn't a real interpreter).
    # Fall back to 'python' on non-Windows / older Python distributions.
    $py = if (Get-Command py -ErrorAction SilentlyContinue) {
        'py'
    } elseif (Get-Command python -ErrorAction SilentlyContinue) {
        'python'
    } else {
        throw "Neither 'py' nor 'python' on PATH. Install Python 3 (winget install Python.Python.3)."
    }

    Write-Host "== Installing build deps via $py =="
    & $py -m pip install --upgrade pip pyinstaller scapy
    if ($LASTEXITCODE -ne 0) { throw 'pip install failed' }

    Write-Host "== Building fremsoft-decoder.exe via $py =="
    # --onefile: single exe; --console: keep stdout for the tail pipe;
    # --hidden-import covers scapy's lazy contrib imports.
    & $py -m PyInstaller `
        --onefile `
        --console `
        --name fremsoft-decoder `
        --hidden-import scapy.contrib.automotive.gm.gmlan `
        --hidden-import scapy.contrib.automotive.uds `
        --workpath build `
        --distpath Output `
        --specpath build `
        $DecoderSrc
    if ($LASTEXITCODE -ne 0) { throw 'PyInstaller failed' }

    $exe = Join-Path $ScriptDir 'Output\fremsoft-decoder.exe'
    if (-not (Test-Path $exe)) { throw "Output exe missing at $exe" }
    $size = [math]::Round((Get-Item $exe).Length / 1MB, 2)
    Write-Host ""
    Write-Host "== Built $exe ($size MB) =="
}
finally {
    Pop-Location
}
