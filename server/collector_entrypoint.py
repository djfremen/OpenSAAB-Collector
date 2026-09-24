"""Add only the Collector inbox to the existing, unchanged public boundary."""
from fastapi import FastAPI
from collector_api import make_collector_router
from public_app import storage
from containment_entrypoint import app as existing_app
collector = FastAPI(docs_url=None, redoc_url=None, openapi_url=None)
collector.include_router(make_collector_router(storage))

@collector.get('/api/collector/status')
def status():
    return {'collector_version':'0.5.0','capture_upload':True,'max_bytes':64*1024*1024,
            'storage':'private','consent':'collector-capture-v1'}

async def app(scope, receive, send):
    if scope.get('type') == 'http' and (scope.get('method'),scope.get('path')) in {
        ('POST','/api/collector/captures'),('GET','/api/collector/status')
    }:
        return await collector(scope, receive, send)
    return await existing_app(scope, receive, send)
