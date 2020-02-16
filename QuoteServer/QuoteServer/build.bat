cd %~dp0
dotnet publish -c Release QuoteServer.csproj /p:PublishProfile=FolderProfile

docker stop quoteserve.seng.uvic.ca
docker rm quoteserve.seng.uvic.ca
docker rmi quote_server_image
docker build -t quote_server_image -f Dockerfile .
docker create -p 4448:4448 --net day_trade_net --name quoteserve.seng.uvic.ca quote_server_image

PAUSE
EXIT
