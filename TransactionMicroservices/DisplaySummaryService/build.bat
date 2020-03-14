cd %~dp0

docker build -t display_summary_service_image -f Dockerfile ../..

PAUSE
EXIT
