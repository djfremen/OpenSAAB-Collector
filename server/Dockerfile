# Deployment overlay preserves the existing website and service boundary.
FROM djfremen/saab-security-api@sha256:1ad4aa2abe4a5ed9b6693227b0d04df4f298bd22b20ed03227a843aeffdf0832
COPY collector_api.py collector_entrypoint.py /app/
CMD ["sh", "-c", "exec uvicorn collector_entrypoint:app --host 0.0.0.0 --port ${PORT:-8000} --no-access-log --limit-concurrency 100 --timeout-keep-alive 5"]
