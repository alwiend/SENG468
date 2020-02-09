cd %~dp0
dotnet publish -c Release SellTriggerService.csproj /p:PublishProfile=FolderProfile

docker stop sell_trigger_service
docker rm sell_trigger_service
docker rmi sell_trigger_service_image
docker build -t sell_trigger_service_image -f Dockerfile .
docker create --net day_trade_net --name sell_trigger_service sell_trigger_service_image

PAUSE
EXIT
