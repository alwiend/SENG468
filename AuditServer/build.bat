cd %~dp0
dotnet publish -c Release AuditServer.csproj /p:PublishProfile=FolderProfile

docker stop audit_server
docker rm audit_server
docker rmi audit_server_image
docker build -t audit_server_image -f Dockerfile .
docker create -p 44439:44439 --net day_trade_net --name audit_server audit_server_image

PAUSE
EXIT
