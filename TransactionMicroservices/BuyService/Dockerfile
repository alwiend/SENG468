﻿FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine3.10 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY TransactionMicroservices/BuyService/*.csproj ./TransactionMicroservices/BuyService/
COPY TransactionMicroservices/BaseService/*.csproj ./TransactionMicroservices/BaseService/
COPY TransactionMicroservices/Database/*.csproj ./TransactionMicroservices/Database/
COPY Common/Utilities/*.csproj ./Common/Utilities/
RUN dotnet restore ./TransactionMicroservices/BuyService

# Copy everything else and build
COPY TransactionMicroservices/BuyService/ ./TransactionMicroservices/BuyService/
COPY TransactionMicroservices/BaseService/ ./TransactionMicroservices/BaseService/
COPY TransactionMicroservices/Database/ ./TransactionMicroservices/Database/
COPY Common/Utilities/ ./Common/Utilities/
RUN dotnet publish -c Release -o out ./TransactionMicroservices/BuyService

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.10
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "BuyService.dll"]