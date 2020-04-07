@echo off
@setlocal

SET t=%1
IF "%~1"=="" SET t=latest

@set "tag=%t%"

docker stack deploy -c dev-stack.yaml dev

