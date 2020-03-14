@echo off
for /f "delims=" %%i in ('docker service ps -f 'name=dev_db.1' dev_db -q --no-trunc') do set output=%db%

echo %db%