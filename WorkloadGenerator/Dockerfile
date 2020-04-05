FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine3.10 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY WorkloadGenerator/*.csproj ./WorkloadGenerator/
RUN dotnet restore ./WorkloadGenerator

# Copy everything else and build
COPY WorkloadGenerator/ ./WorkloadGenerator
RUN dotnet publish -c Release -o out ./WorkloadGenerator

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.10
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "WorkloadGenerator.dll"]
