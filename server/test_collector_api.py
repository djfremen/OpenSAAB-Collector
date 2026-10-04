import hashlib,io,json,struct,zipfile,runpy,sys,types
from pathlib import Path
import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from collector_api import make_collector_router, validate_bundle, CONSENT, VERSION, ADAPTER_MODELS

class Store:
    def __init__(self): self.objects={};self.fail=False;self.head_overrides={}
    def put_object(self,**kw):
        if self.fail: raise RuntimeError('private credentials must not leak')
        self.objects[kw['Key']]=kw
    def head_object(self,**kw):
        o=self.objects[kw['Key']]
        return {'ContentLength':len(o['Body']),'Metadata':o['Metadata'],'ContentDisposition':o.get('ContentDisposition'),**self.head_overrides}

def bundle(address=4, actual=4, truncated=False, extra=None, consent=CONSENT, metadata=None):
    usb=struct.pack('<HQIHBHHBBI',27,0,0,9,0,1,actual,0x81,3,2)+b'\x01\x02'
    pcap=struct.pack('<IHHIIII',0xa1b2c3d4,2,4,0,0,65535,249)+struct.pack('<IIII',0,0,len(usb),len(usb)+int(truncated))+usb
    session={k:'' for k in ['collector_version','adapter','device_label','usb_interface','started_utc','stopped_utc','os','stop_reason']}
    session.update(format=1,consent=consent,usb_address=address,usb_interface=r'\\.\USBPcap1',capture_state='stopped_gracefully',diagnostic_success='not inferred',capture_sha256=hashlib.sha256(pcap).hexdigest())
    if metadata:session.update(metadata)
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
    assert value['summary']=={'packets':1,'bulk_payload_packets':1}
    stored=next(iter(s.objects.values()))
    assert stored['Metadata']=={'sha256':hashlib.sha256(b).hexdigest(),'consent':CONSENT}
    assert 'ContentDisposition' not in stored

def identity(model='Chipsoft',**changes):
    value={'adapter_model':model,'capture_id':'OpenSAAB_'+model+'_20261004_073025Z_abcdef01','started_utc':'2026-10-04T07:30:25.1234567Z'}
    value.update(changes)
    return value

@pytest.mark.parametrize('model',['Chipsoft','MDI','Mongoose','Nano'])
def test_selected_model_identity_and_filename_preserve_hash_receipt(model):
    c,s=client();meta=identity(model);b=bundle(metadata=meta);r=post(c,b)
    assert r.status_code==201;value=r.json();digest=hashlib.sha256(b).hexdigest()
    assert value['receipt']=='OSCAP-'+digest
    assert value['summary']['adapter_model']==model
    assert value['summary']['capture_id']==meta['capture_id']
    assert value['summary']['capture_filename']==meta['capture_id']+'.zip'
    stored=s.objects['collector/v1/OSCAP-'+digest+'.zip']
    assert stored['Body']==b
    assert stored['Metadata']=={'sha256':digest,'consent':CONSENT,'adapter_model':model,
                               'capture_id':meta['capture_id'],'capture_filename':meta['capture_id']+'.zip'}
    assert stored['ContentDisposition']=='attachment; filename="'+meta['capture_id']+'.zip"'
    retry=post(c,b);assert retry.status_code==201
    assert retry.json()['receipt']==value['receipt'];assert len(s.objects)==1

@pytest.mark.parametrize('metadata',[
    {'adapter_model':'Nano'},
    {'capture_id':'OpenSAAB_Nano_20261004_073025Z_abcdef01'},
    identity('Other'),
    identity('chipsoft'),
    identity('Nano',capture_id='OpenSAAB_MDI_20261004_073025Z_abcdef01'),
    identity(capture_id='OpenSAAB_Chipsoft_20261004_073025Z_ABCDEF01'),
    identity(capture_id='OpenSAAB_Chipsoft_20261004_073025Z_abcdef01.zip'),
    identity(capture_id='../OpenSAAB_Chipsoft_20261004_073025Z_abcdef01'),
    identity(capture_id='OpenSAAB_Chipsoft_20261304_073025Z_abcdef01'),
    identity(started_utc='2026-10-04T07:30:26Z'),
    identity(started_utc='2026-10-04T08:30:25+01:00'),
    identity(started_utc='2026-10-04T07:30:25'),
    identity(started_utc='not a timestamp'),
    identity(capture_filename='private.zip'),
])
def test_invalid_selected_identity_rejected_before_storage(metadata):
    c,s=client();r=post(c,bundle(metadata=metadata))
    assert r.status_code==400;assert not s.objects

def test_selected_identity_accepts_explicit_zero_utc_offset():
    assert validate_bundle(bundle(metadata=identity(started_utc='2026-10-04T07:30:25+00:00')))['adapter_model']=='Chipsoft'

@pytest.mark.parametrize('overrides',[
    {'Metadata':{'sha256':'0'*64}},
    {'ContentDisposition':'attachment; filename="wrong.zip"'},
])
def test_selected_identity_unverified_head_is_not_success(overrides):
    c,s=client();s.head_overrides=overrides;r=post(c,bundle(metadata=identity()))
    assert r.status_code==503;assert r.json()['detail']=='Upload was not confirmed; keep your local copy and retry'

@pytest.mark.parametrize('field',['adapter_model','capture_id','capture_filename'])
def test_selected_identity_metadata_mismatch_is_not_success(field):
    c,s=client();meta=identity();b=bundle(metadata=meta)
    stored={'sha256':hashlib.sha256(b).hexdigest(),'consent':CONSENT,'adapter_model':meta['adapter_model'],
            'capture_id':meta['capture_id'],'capture_filename':meta['capture_id']+'.zip'}
    stored[field]='wrong';s.head_overrides={'Metadata':stored}
    assert post(c,b).status_code==503

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

def test_status_advertises_filename_capability_and_preserves_other_routes(monkeypatch):
    existing=FastAPI()
    @existing.get('/existing/status')
    def existing_status():return {'existing_boundary':True}
    public=types.ModuleType('public_app');public.storage=lambda:Store()
    boundary=types.ModuleType('containment_entrypoint');boundary.app=existing
    monkeypatch.setitem(sys.modules,'public_app',public)
    monkeypatch.setitem(sys.modules,'containment_entrypoint',boundary)
    app=runpy.run_path(str(Path(__file__).with_name('collector_entrypoint.py')))['app']
    c=TestClient(app);response=c.get('/api/collector/status')
    assert response.status_code==200
    assert response.json()=={'collector_version':VERSION,'capture_upload':True,'max_bytes':64*1024*1024,
                            'storage':'private','consent':CONSENT,'adapter_labelled_filenames':True,
                            'adapter_models':list(ADAPTER_MODELS)}
    assert c.get('/existing/status').json()=={'existing_boundary':True}
