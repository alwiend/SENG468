# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1.1-alpine3.10
WORKDIR /app

COPY ./bin/Release/netcoreapp3.1/publish .
ENTRYPOINT ["dotnet", "SellTriggerService.dll"]