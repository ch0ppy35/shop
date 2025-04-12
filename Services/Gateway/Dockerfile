FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /source
COPY nats-shop.sln .
COPY Services/Common/Common.csproj ./Services/Common/
COPY Services/Gateway/Gateway.csproj ./Services/Gateway/

RUN dotnet restore Services/Gateway/Gateway.csproj

COPY Services/Common/. ./Services/Common/
COPY Services/Gateway/. ./Services/Gateway/

RUN dotnet publish Services/Gateway/Gateway.csproj -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "Gateway.dll"]