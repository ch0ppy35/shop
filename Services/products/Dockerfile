FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /source
COPY nats-shop.sln .
COPY Services/Common/Common.csproj ./Services/Common/
COPY Services/Products/Products.csproj ./Services/Products/

RUN dotnet restore Services/Products/Products.csproj

COPY Services/Common/. ./Services/Common/
COPY Services/Products/. ./Services/Products/

RUN dotnet publish Services/Products/Products.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Products.dll"]
