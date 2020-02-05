cd %~dp0
dotnet publish -c Release SellService.csproj /p:PublishProfile=FolderProfile

docker stop sell_service
docker rm sell_service
docker rmi sell_service_image
docker build -t sell_service_image -f Dockerfile .
docker create -p 44442:44442 -p 44443:44443 -p 44444:44444 --net day_trade_net --name sell_service sell_service_image

PAUSE
EXITff