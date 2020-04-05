cd %~dp0

docker build -t bberthelet/workload_generator_image:latest -f Dockerfile ..

PAUSE
EXIT
