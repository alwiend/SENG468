cd %~dp0
dotnet publish -c Release AddService.csproj /p:PublishProfile=FolderProfile

docker stop add_service
docker rm add_service
docker rmi add_service_image
docker build -t add_service_image -f Dockerfile .
docker create -p 44441:44441 --net day_trade_net --name add_service add_service_image

PAUSE
EXIT
