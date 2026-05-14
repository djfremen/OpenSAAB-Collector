# fremsoft-decoder.exe — license notice

`fremsoft-decoder.exe` shipped inside the OpenSAAB Collector installer
is a PyInstaller-bundled distribution of:

- **`Chipsoft_RE/tools/shim_log_decode.py`** — the small dissection
  wrapper we wrote ourselves. Apache 2.0 like the rest of OpenSAAB.
- **scapy** ([github.com/secdev/scapy](https://github.com/secdev/scapy))
  — packet manipulation library. **GPL-2.0-only.**
- **CPython runtime** + standard library — Python Software Foundation
  License (PSF), GPL-compatible.

Because scapy is GPL-2.0, the resulting `fremsoft-decoder.exe`
**inherits GPL-2.0**. It is shipped alongside the rest of the Collector
(which remains Apache 2.0) as a separate process — *mere aggregation*
in GPL-2.0 §0 terms — not linked into any other binary.

Practically: you can use, redistribute, and modify
`fremsoft-decoder.exe` under GPL-2.0. The other Collector binaries
(`OpenSAAB.Collector.Service.exe`, `OpenSAAB.Collector.Tray.exe`, the
two interception DLLs) remain Apache 2.0 and contain no scapy / GPL
code.

## Source

- Decoder wrapper:
  <https://github.com/djfremen/Chipsoft_RE/blob/main/tools/shim_log_decode.py>
- scapy: <https://github.com/secdev/scapy> (`pip install scapy`)
- CPython: <https://www.python.org>

## Why bundle GPL into an Apache distribution at all

Real-time UDS dissection requires a full GMLAN/UDS dissector, and
scapy's contrib module is the most complete open-source one we
found. Re-implementing it natively in C# would be substantial work
and we'd lose for-free updates as scapy adds new SIDs / NRC names.
Bundling as a separate process keeps the licensing clean while
giving end users the live decoded view.
