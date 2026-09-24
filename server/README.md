# Private Collector inbox

`collector_api.py` accepts `POST /api/collector/captures` with a ZIP, SHA-256 header
and `X-OpenSAAB-Consent: collector-capture-v1`. Only three files are permitted:
`usb.pcap`, `session.json`, `actions.jsonl`. ZIP and expanded content are limited
to 64 MiB, notes/metadata to smaller limits. Pcap link type, record boundaries,
USB address, payload lengths and checksum are checked before storage.

Credentials are loaded server-side from the existing environment:
`S3_ENDPOINT_URL`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`. Never put these in
source, build arguments, images or client configuration. The destination is the
existing private `opensaab-capture` bucket under `collector/v1/OSCAP-<sha256>.zip`.

The endpoint accepts contributor uploads without an account. It exposes **no
listing, raw-file download or deletion API**. Limits are deliberately conservative:
2 simultaneous uploads, 6 submissions per address/hour, 40 total/hour, 256 MiB/hour,
120-second request-body timeout. Quotas are process-local and reset on restart;
keep the current single-worker/single-instance deployment. Shared persistent
quotas or edge abuse controls are required before scaling. Proxy addresses may
cause contributors to share a quota. Retrying identical bytes reuses the same
object key. No file is deleted from the contributor's computer.

The response includes the receipt, SHA-256, byte length and capture summary only
after R2 acknowledges the object and a HEAD check verifies length and metadata.
A storage failure returns 503 and never reports success. Error responses omit
credentials and raw exception text. Logs should not record request bodies.

## Deployment

The Dockerfile is a narrow overlay on the existing verified website image.
`collector_entrypoint.py` adds exactly the Collector POST and status GET routes;
all other traffic continues through the existing service boundary. It does not
restore `/ingest/shim-log` or change security, metadata or Android support routes.
Deploy the built image by digest, preserving current runtime environment values.
The pinned base image belongs to the maintainer's registry; contributors can
build the desktop app without that image.

Test with `python -m pytest server/test_collector_api.py`. Tests use a fake store;
no secrets or real captures are required. Verify a private R2 round-trip and an
anonymous-read denial separately before publishing a client release.
