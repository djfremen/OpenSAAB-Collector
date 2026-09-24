import hashlib,io,json,struct,zipfile
import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from collector_api import make_collector_router, validate_bundle, CONSENT

class Store:
    def __init__(self): self.objects={};self.fail=False
    def put_object(self,**kw):
        if self.fail: raise RuntimeError('private credentials must not leak')
        self.objects[kw['Key']]=kw
    def head_object(self,**kw):
        o=self.objects[kw['Key']]
        return {'ContentLength':len(o['Body']),'Metadata':o['Metadata']}

def bundle(address=4, actual=4, truncated=False, extra=None, consent=CONSENT):
    usb=struct.pack('<HQIHBHHBBI',27,0,0,9,0,1,actual,0x81,3,2)+b'\x01\x02'
    pcap=struct.pack('<IHHIIII',0xa1b2c3d4,2,4,0,0,65535,249)+struct.pack('<IIII',0,0,len(usb),len(usb)+int(truncated))+usb
    session={k:'' for k in ['collector_version','adapter','device_label','usb_interface','started_utc','stopped_utc','os','stop_reason']}
    session.update(format=1,consent=consent,usb_address=address,usb_interface=r'\\.\USBPcap1',capture_state='stopped_gracefully',diagnostic_success='not inferred',capture_sha256=hashlib.sha256(pcap).hexdigest())
    b=io.BytesIO()
    with zipfile.ZipFile(b,'w',zipfile.ZIP_DEFLATED) as z:
        z.writestr('usb.pcap',pcap);z.writestr('session.json',json.dumps(session));z.writestr('actions.jsonl','')
        if extra:z.writestr(extra,'bad')
    return b.getvalue()

def client():
    s=Store();a=FastAPI();a.include_router(make_collector_router(lambda:s));return TestClient(a),s

def post(c,b,**headers):
    h={'Content-Type':'application/zip','X-OpenSAAB-Consent':CONSENT,'X-Content-SHA256':hashlib.sha256(b).hexdigest()};h.update(headers)
    return c.post('/api/collector/captures',content=b,headers=h)

def test_valid_roundtrip_and_retry():
    c,s=client();b=bundle();r=post(c,b);assert r.status_code==201;value=r.json();assert value['stored'] and value['bytes']==len(b);assert value['summary']['packets']==1
    assert post(c,b).json()['receipt']==value['receipt'];assert len(s.objects)==1
    assert next(iter(s.objects.values()))['Body']==b

@pytest.mark.parametrize('kwargs',[{'actual':5},{'truncated':True},{'extra':'../secret.txt'},{'consent':'old'}])
def test_rejects_bad_bundles(kwargs):
    c,s=client();assert post(c,bundle(**kwargs)).status_code==400;assert not s.objects

def test_consent_and_checksum():
    c,s=client();b=bundle();assert post(c,b,**{'X-Content-SHA256':'0'*64}).status_code==400
    assert post(c,b,**{'X-OpenSAAB-Consent':'no'}).status_code==400;assert not s.objects

def test_storage_failure_is_not_success_or_secret():
    c,s=client();s.fail=True;r=post(c,bundle());assert r.status_code==503;assert 'credentials' not in r.text;assert not s.objects

def test_rate_limit():
    c,s=client();b=bundle()
    for _ in range(6):assert post(c,b).status_code==201
    assert post(c,b).status_code==429

def test_malformed_zip():
    c,s=client();assert post(c,b'not a zip').status_code==400;assert not s.objects

def test_decompression_size_limit():
    b=io.BytesIO()
    with zipfile.ZipFile(b,'w',zipfile.ZIP_DEFLATED) as z:
        z.writestr('usb.pcap',b'0'*(64*1024*1024+1));z.writestr('session.json','{}');z.writestr('actions.jsonl','')
    with pytest.raises(ValueError):validate_bundle(b.getvalue())
