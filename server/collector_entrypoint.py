"""Add only the Collector inbox to the existing, unchanged public boundary."""
from fastapi import FastAPI
from collector_api import make_collector_router, VERSION, ADAPTER_MODELS, MAX_BYTES, CONSENT
from public_app import storage
from containment_entrypoint import app as existing_app
collector = FastAPI(docs_url=None, redoc_url=None, openapi_url=None)
collector.include_router(make_collector_router(storage))

@collector.get('/api/collector/status')
def status():
    return {'collector_version':VERSION,'capture_upload':True,'max_bytes':MAX_BYTES,
            'storage':'private','consent':CONSENT,'adapter_labelled_filenames':True,
            'adapter_models':list(ADAPTER_MODELS)}

async def app(scope, receive, send):
    if scope.get('type') == 'http' and (scope.get('method'),scope.get('path')) in {
        ('POST','/api/collector/captures'),('GET','/api/collector/status')
    }:
        return await collector(scope, receive, send)
    return await existing_app(scope, receive, send)
