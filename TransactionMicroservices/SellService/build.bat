cd %~dp0
dotnet publish -c Release SellService.csproj /p:PublishProfile=FolderProfile

docker stop sell_service
docker rm sell_service
docker rmi sell_service_image
docker build -t sell_service_image -f Dockerfile .
docker create --net day_trade_net --name sell_service sell_service_image

PAUSE
EXIT