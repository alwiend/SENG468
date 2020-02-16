cd %~dp0
dotnet publish -c Release QuoteService.csproj /p:PublishProfile=FolderProfile

docker stop quote_service
docker rm quote_service
docker rmi quote_service_image
docker build -t quote_service_image -f Dockerfile .
docker create -p 44440:44440 --net day_trade_net --name quote_service quote_service_image

PAUSE
EXIT
