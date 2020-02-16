cd %~dp0
dotnet publish -c Release DisplaySummaryService.csproj /p:PublishProfile=FolderProfile

docker stop display_summary_service
docker rm display_summary_service
docker rmi display_summary_service_image
docker build -t display_summary_service_image -f Dockerfile .
docker create -p 44445:44445 --net day_trade_net --name display_summary_service display_summary_service_image

PAUSE
EXIT