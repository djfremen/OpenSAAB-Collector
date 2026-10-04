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

## Selected adapter and capture filename

Older format-1 sessions with the original metadata fields remain accepted. New
sessions may add the paired `adapter_model` and `capture_id` fields. Both are
required together; other additional fields remain rejected. `adapter_model` is
exactly one of `Chipsoft`, `MDI`, `Mongoose`, or `Nano`. This is the contributor's
selection, not automatic USB identification or evidence of adapter compatibility.
The existing free-text `adapter` field remains separate.

The capture ID is `OpenSAAB_<model>_<yyyyMMdd_HHmmss>Z_<8 lowercase hex>`, for
example `OpenSAAB_MDI_20261004_073025Z_abcdef01`. Its model must agree with
`adapter_model`; its real calendar date/time must match the UTC `started_utc`
to the second. A fractional UTC start time is permitted. Non-UTC/naive timestamps,
path separators, arbitrary labels, uppercase suffixes and mismatched start times
are rejected. The ZIP download filename is derived as `<capture_id>.zip`; a
client-supplied filename field is not accepted.

For these identified captures, the response summary and object metadata include
`adapter_model`, `capture_id` and `capture_filename`. Storage also sets
`Content-Disposition: attachment; filename="<capture_id>.zip"`, so an authorized
private download that honors this header can use the friendly filename. HEAD
verification checks those identity fields and the disposition before success.
Legacy uploads retain their original metadata and response shape.

The actual object key stays `collector/v1/OSCAP-<sha256>.zip`, and the receipt is
still the SHA-256 of the exact uploaded ZIP bytes. Identical retries therefore
reuse the same key. Friendly names are metadata/download hints, not alternate
storage keys or proof of hardware success. An owner indexing/export tool can use
these metadata fields without opening raw captures; the API adds no public
listing/download route. Renaming a local ZIP alone does not change its receipt.

## Deployment

The Dockerfile is a narrow overlay on the existing verified website image.
`collector_entrypoint.py` adds exactly the Collector POST and status GET routes;
all other traffic continues through the existing service boundary. It does not
restore `/ingest/shim-log` or change security, metadata or Android support routes.
Deploy the built image by digest, preserving current runtime environment values.
The pinned base image belongs to the maintainer's registry; contributors can
build the desktop app without that image.

The new metadata requires this backward-compatible validator before new clients
upload identified sessions: the earlier exact-field validator rejects them.
Production deployment is a separate reviewed action. Build any overlay from the
currently active immutable image and preserve newer iOS/Android support-schema,
website and other service changes; an old documented base image is not authority
to roll them back. Qualification must verify old and new bundles against the
deployed validator and independently read back the friendly metadata/disposition.
Local fake-store tests are not a deployed R2 round-trip.

Test with `python -m pytest server/test_collector_api.py`. Tests use a fake store;
no secrets or real captures are required. Verify a private R2 round-trip and an
anonymous-read denial separately before publishing a client release.
