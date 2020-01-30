cd %~dp0
dotnet publish -c Release WebServer.csproj /p:PublishProfile=Properties\PublishProfiles\FolderProfile.pubxml

docker stop web_server
docker rm web_server
docker rmi web_server_image
docker build --no-cache -t web_server_image -f Dockerfile .
docker create -p 8080:80 --net day_trade_net --name web_server web_server_image

PAUSE
EXIT
