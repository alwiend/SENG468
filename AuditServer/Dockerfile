FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine3.10 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY AuditServer/*.csproj ./AuditServer/
COPY Common/Utilities/*.csproj ./Common/Utilities/
RUN dotnet restore ./AuditServer

# Copy everything else and build
COPY AuditServer/ ./AuditServer
COPY Common/Utilities/ ./Common/Utilities
RUN dotnet publish -c Release -o out ./AuditServer

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.10
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "AuditServer.dll"]