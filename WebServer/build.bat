cd %~dp0
dotnet publish WebServer.csproj /p:PublishProfile=FolderProfile

docker stop web_server
docker rm web_server
docker rmi web_server_image
docker build -t web_server_image -f Dockerfile .
docker create --net day_trade_net --ip 172.1.1.10 --name web_server web_server_image

PAUSE
EXIT
