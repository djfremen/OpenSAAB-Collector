# Manual USBPcap fallback

> v0.4.0 dropped USBPcap from the installer. The genuine Chipsoft driver's
> native logging is the only first-class capture path. This note exists for
> the case where, on some machine, native logging proves unreliable and you
> want raw USB-level evidence.

USBPcap captures the wire bytes between Tech2Win and the Chipsoft adapter at
the OS level. It does not require — and cannot be replaced by — anything in
this repo; the Collector simply ingests whatever you point at it.

## When to reach for it

- Native `options.json` `LogLevel: 0` is set, you've run a session, and the
  driver's `*.log` file is empty or missing.
- You suspect the driver is throwing logs away (Boost.Log sink unstable on
  the user's machine).
- You want byte-perfect ground truth that bypasses the driver's logging
  pipeline entirely.

## One-time install

USBPcap is a free kernel-mode driver from <https://desowin.org/usbpcap/>.
~3 MB; one reboot. After install, `USBPcapCMD.exe` lands in
`C:\Program Files\USBPcap\` (or `Program Files (x86)\` on some systems).

## Capture

From an Administrator PowerShell:

```pwsh
# 1. List USB hubs. Pick the one whose subtree contains your Chipsoft
#    adapter — usually the integrated USB controller.
& "C:\Program Files\USBPcap\USBPcapCMD.exe"

# 2. Capture the chosen hub to a .pcapng file. -A captures the whole hub
#    (any device under it), which is the safest pick when you don't know
#    exactly which port the Chipsoft is on.
& "C:\Program Files\USBPcap\USBPcapCMD.exe" `
    -d \\.\USBPcap1 -A `
    -o "$env:USERPROFILE\Desktop\chipsoft_session.pcapng"

# 3. Run your Tech2Win / J2534 session.
# 4. Ctrl+C in the USBPcap window to stop.
```

Open the `.pcapng` in Wireshark to sanity-check (look for `USB Bulk` /
`USB Interrupt` frames from the Chipsoft device's VID/PID).

## Ship it to OpenSAAB

The Collector's server `/ingest/shim-log` endpoint accepts any gzipped
capture with a `X-Capture-Source` header. To send a USBPcap manually:

```pwsh
$installId = (Get-ItemProperty 'HKLM:\SOFTWARE\OpenSAAB\Collector').InstallId
$pcap = "$env:USERPROFILE\Desktop\chipsoft_session.pcapng"
$gz = "$pcap.gz"

# gzip
$in  = [IO.File]::OpenRead($pcap)
$out = [IO.File]::OpenWrite($gz)
$cmp = New-Object IO.Compression.GZipStream($out, [IO.Compression.CompressionLevel]::Optimal)
$in.CopyTo($cmp); $cmp.Close(); $out.Close(); $in.Close()

# upload
$bytes = [IO.File]::ReadAllBytes($gz)
$sha   = (Get-FileHash $gz -Algorithm SHA256).Hash.ToLower()
Invoke-RestMethod -Method Post `
  -Uri 'https://relevant-diann-djfremen2-c013cdc3.koyeb.app/ingest/shim-log' `
  -ContentType 'application/octet-stream' `
  -Body $bytes `
  -Headers @{
      'X-Install-ID'        = $installId
      'X-Capture-Source'    = 'usbpcap'
      'X-Consent-Version'   = 'v2'
      'X-Collector-Version' = '0.4.0-manual'
      'X-Content-SHA256'    = $sha
  }
```

That puts the capture under `uploads/<install-id>/usbpcap_<wall_ms>.pcapng.gz`
on the server, alongside the native driver logs.

## Why we removed it from the installer

- USBPcap is the heaviest install step the Collector ever asked for: a
  kernel-mode driver, a reboot, and frequent AV/SmartScreen prompts.
- For 100% of captures we've collected to date, native driver logging
  contains the same information, decoded with `decode_chipsoft_log.py`.
- Carrying a second supervisor in the service to spawn `USBPcapCMD.exe`
  was the source of multiple v0.2.x bugs (orphan processes, malformed
  SHB-only pcaps, FileSystemWatcher death). All of that is gone in
  v0.4.0.

If you find a real case where native logging is silent but USBPcap
captures the same bus, open an issue — that's a regression worth
investigating in the driver path before we ever re-bundle USBPcap.
