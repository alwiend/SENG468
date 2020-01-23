cd %~dp0
dotnet publish QuoteService.csproj /p:PublishProfile=FolderProfile

docker stop quote_service
docker rm quote_service
docker rmi quote_service_image
docker build -t quote_service_image -f Dockerfile .
docker create --net day_trade_net --ip 172.1.0.11 --name quote_service quote_service_image

PAUSE
EXIT
