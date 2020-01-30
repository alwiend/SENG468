# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.1-alpine3.10
WORKDIR /app

COPY ./bin/Release/netcoreapp3.1/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "WebServer.dll"]
