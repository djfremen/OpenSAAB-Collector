# Two-file Collector overlay; verify the current live image before deployment.
FROM djfremen/saab-security-api@sha256:668f5d029bc9e768cedd8d3450d4cb7f81310449c4bd343000023b65b662ad21
COPY --chown=65532:65532 collector_api.py collector_entrypoint.py /app/
# Inherit the verified base user, command, website and runtime environment.
