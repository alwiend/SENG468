cd %~dp0
@ECHO OFF

SET tag=latest

IF NOT "%~1"=="" SET tag=%1

docker build -t bberthelet/add_service_image:%tag% -f Dockerfile ../..

PAUSE
EXIT
