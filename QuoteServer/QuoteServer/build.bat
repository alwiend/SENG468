cd %~dp0

docker build -t bberthelet/quote_server_image -f Dockerfile ../..

PAUSE
EXIT
