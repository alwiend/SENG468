cd %~dp0
dotnet publish QuoteServer.csproj /p:PublishProfile=FolderProfile

docker stop quote_server
docker rm quote_server
docker rmi quote_server_image
docker build -t quote_server_image -f Dockerfile .
docker create --net day_trade_net --ip 172.1.0.10 --name quote_server quote_server_image

PAUSE
EXIT
