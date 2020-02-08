cd %~dp0
dotnet publish -c Release BuyTriggerService.csproj /p:PublishProfile=FolderProfile

docker stop buy_trigger_service
docker rm buy_trigger_service
docker rmi buy_trigger_service_image
docker build -t buy_trigger_service_image -f Dockerfile .
docker create --net day_trade_net --name buy_trigger_service buy_trigger_service_image

PAUSE
EXIT
