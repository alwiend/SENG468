cd %~dp0
dotnet publish -c Release BuyService.csproj /p:PublishProfile=FolderProfile

docker stop buy_service
docker rm buy_service
docker rmi buy_service_image
docker build -t buy_service_image -f Dockerfile .
docker create -p 44442:44442 -p 44443:44443 -p 44444:44444 --net day_trade_net --name buy_service buy_service_image

PAUSE
EXITff