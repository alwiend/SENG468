FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine3.10 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY TransactionMicroservices/SellService/*.csproj ./TransactionMicroservices/SellService/
COPY TransactionMicroservices/BaseService/*.csproj ./TransactionMicroservices/BaseService/
COPY TransactionMicroservices/Database/*.csproj ./TransactionMicroservices/Database/
COPY Common/Utilities/*.csproj ./Common/Utilities/
COPY Common/Constants/*.csproj ./Common/Constants/
RUN dotnet restore ./TransactionMicroservices/SellService

# Copy everything else and build
COPY TransactionMicroservices/SellService/ ./TransactionMicroservices/SellService/
COPY TransactionMicroservices/BaseService/ ./TransactionMicroservices/BaseService/
COPY TransactionMicroservices/Database/ ./TransactionMicroservices/Database/
COPY Common/Utilities/ ./Common/Utilities/
COPY Common/Constants/ ./Common/Constants/
RUN dotnet publish -c Release -o out ./TransactionMicroservices/SellService

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.10
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "SellService.dll"]