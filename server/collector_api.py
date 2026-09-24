"""Private USBPcap submissions. No adapter commands, shims or public downloads."""
import asyncio
import hashlib
import io
import json
import re
import struct
import threading
import time
import zipfile
from collections import deque
from fastapi import APIRouter, HTTPException, Request
from starlette.concurrency import run_in_threadpool

MAX_BYTES = 64 * 1024 * 1024
CONSENT = 'collector-capture-v1'
VERSION = '0.5.0'


def validate_pcap(data, address):
    if len(data) < 24 or struct.unpack_from('<IHH', data) != (0xa1b2c3d4, 2, 4):
        raise ValueError('Invalid pcap header')
    if struct.unpack_from('<I', data, 20)[0] != 249:
        raise ValueError('Not a USBPcap file')
    pos = 24
    packets = bulk = 0
    while pos < len(data):
        if len(data) - pos < 16:
            raise ValueError('Incomplete packet header')
        _, _, included, original = struct.unpack_from('<IIII', data, pos)
        pos += 16
        if included != original or not 27 <= included <= 65535 or included > len(data) - pos:
            raise ValueError('Incomplete packet')
        header = struct.unpack_from('<H', data, pos)[0]
        dev = struct.unpack_from('<H', data, pos + 19)[0]
        transfer = data[pos + 22]
        payload = struct.unpack_from('<I', data, pos + 23)[0]
        if not 27 <= header <= included or payload > included - header or dev != address:
            raise ValueError('Unexpected USB device or invalid payload')
        bulk += int(transfer == 3 and payload > 0)
        packets += 1
        pos += included
    if not packets:
        raise ValueError('Empty capture')
    return {'packets': packets, 'bulk_payload_packets': bulk}


def validate_bundle(body):
    if not body or len(body) > MAX_BYTES:
        raise ValueError('Invalid bundle size')
    with zipfile.ZipFile(io.BytesIO(body)) as z:
        entries = z.infolist()
        if len(entries) != 3 or {i.filename for i in entries} != {'usb.pcap', 'session.json', 'actions.jsonl'}:
            raise ValueError('Unexpected bundle files')
        if sum(i.file_size for i in entries) > MAX_BYTES:
            raise ValueError('Expanded bundle is too large')
        for i in entries:
            if i.flag_bits & 1 or i.compress_type not in (zipfile.ZIP_STORED, zipfile.ZIP_DEFLATED):
                raise ValueError('Unsupported ZIP format')
        if z.getinfo('session.json').file_size > 16384 or z.getinfo('actions.jsonl').file_size > 65536:
            raise ValueError('Notes are too large')
        session = json.loads(z.read('session.json'))
        fields = {'format','collector_version','adapter','device_label','usb_interface','usb_address',
                  'started_utc','stopped_utc','os','capture_state','stop_reason','consent','diagnostic_success','capture_sha256'}
        if not isinstance(session, dict) or set(session) != fields:
            raise ValueError('Invalid session fields')
        if session['format'] != 1 or session['consent'] != CONSENT or session['capture_state'] != 'stopped_gracefully':
            raise ValueError('Incomplete capture or missing consent')
        for k,v in session.items():
            if k not in ('format','usb_address') and (not isinstance(v,str) or len(v)>1024):
                raise ValueError('Invalid metadata')
        address = session['usb_address']
        if type(address) is not int or not 1 <= address <= 127:
            raise ValueError('Invalid USB address')
        if not re.fullmatch(r'\\\\\.\\USBPcap[0-9]+', session['usb_interface']):
            raise ValueError('Invalid USB interface')
        notes = z.read('actions.jsonl').decode('utf-8-sig')
        lines = [line for line in notes.splitlines() if line.strip()]
        if len(lines)>200:
            raise ValueError('Too many notes')
        for line in lines:
            note = json.loads(line)
            if not isinstance(note,dict) or set(note)!={'utc','action'} or any(not isinstance(v,str) or len(v)>512 for v in note.values()):
                raise ValueError('Invalid action note')
        capture = z.read('usb.pcap')
        if hashlib.sha256(capture).hexdigest() != session['capture_sha256']:
            raise ValueError('Capture checksum mismatch')
        summary = validate_pcap(capture, address)
    return summary


class Limits:
    def __init__(self):
        self.lock=threading.Lock()
        self.entries=deque()
        self.active=threading.BoundedSemaphore(2)
    def accept(self, address, size):
        now=time.monotonic()
        with self.lock:
            while self.entries and self.entries[0][0] < now-3600:
                self.entries.popleft()
            if len(self.entries)>=40 or sum(x[2] for x in self.entries)+size>256*1024*1024 or sum(x[1]==address for x in self.entries)>=6:
                raise HTTPException(429,'Upload limit reached; keep your file and retry later',headers={'Retry-After':'3600'})
            self.entries.append((now,address,size))


def make_collector_router(storage, bucket=lambda:'opensaab-capture'):
    router=APIRouter()
    limits=Limits()
    def save(body, digest):
        try:
            summary=validate_bundle(body)
        except (ValueError,KeyError,TypeError,UnicodeError,zipfile.BadZipFile,RuntimeError,OverflowError,RecursionError):
            raise HTTPException(400,'Capture bundle is invalid; keep the local files') from None
        client=storage()
        if client is None:
            raise HTTPException(503,'Private storage unavailable; keep your local copy')
        receipt='OSCAP-'+digest
        key='collector/v1/'+receipt+'.zip'
        try:
            client.put_object(Bucket=bucket(),Key=key,Body=body,ContentType='application/zip',Metadata={'sha256':digest,'consent':CONSENT})
            stored=client.head_object(Bucket=bucket(),Key=key)
            if stored.get('ContentLength')!=len(body) or stored.get('Metadata',{}).get('sha256')!=digest:
                raise RuntimeError('Storage verification failed')
        except Exception:
            raise HTTPException(503,'Upload was not confirmed; keep your local copy and retry') from None
        return {'stored':True,'receipt':receipt,'sha256':digest,'bytes':len(body),'summary':summary}

    @router.post('/api/collector/captures',status_code=201)
    async def upload(request:Request):
        if request.headers.get('X-OpenSAAB-Consent')!=CONSENT:
            raise HTTPException(400,'Capture upload consent is required')
        if request.headers.get('content-type','').split(';')[0].strip()!='application/zip':
            raise HTTPException(415,'Expected a capture ZIP')
        digest=request.headers.get('X-Content-SHA256','')
        length=request.headers.get('content-length','')
        if not re.fullmatch('[a-f0-9]{64}',digest) or not length.isdigit():
            raise HTTPException(400,'Checksum and content length are required')
        size=int(length)
        if not 1<=size<=MAX_BYTES:
            raise HTTPException(413,'Capture bundle exceeds 64 MiB')
        limits.accept(request.client.host if request.client else 'unknown',size)
        if not limits.active.acquire(blocking=False):
            raise HTTPException(429,'Two uploads are already in progress; retry shortly')
        try:
            body=bytearray()
            async def read_body():
                async for chunk in request.stream():
                    body.extend(chunk)
                    if len(body)>size:
                        raise HTTPException(413,'Body exceeds declared length')
            try:
                await asyncio.wait_for(read_body(),timeout=120)
            except asyncio.TimeoutError:
                raise HTTPException(408,'Upload timed out; keep your local copy and retry') from None
            if len(body)!=size or hashlib.sha256(body).hexdigest()!=digest:
                raise HTTPException(400,'Upload checksum mismatch')
            return await run_in_threadpool(save,bytes(body),digest)
        finally:
            limits.active.release()
    return router
