cd %~dp0

docker build -t quote_service_image -f Dockerfile ../..

PAUSE
EXIT
