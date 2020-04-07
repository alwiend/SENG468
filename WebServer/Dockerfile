FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine3.10 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY WebServer/*.csproj ./WebServer/
COPY Common/Utilities/*.csproj ./Common/Utilities/
RUN dotnet restore ./WebServer

# Copy everything else and build
COPY WebServer/ ./WebServer
COPY Common/Utilities/ ./Common/Utilities
RUN dotnet publish -c Release -o out ./WebServer

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.10
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "WebServer.dll"]