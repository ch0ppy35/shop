FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /source
COPY nats-shop.sln .
COPY Services/Common/Common.csproj ./Services/Common/
COPY Services/Recommendations/Recommendations.csproj ./Services/Recommendations/

RUN dotnet restore Services/Recommendations/Recommendations.csproj

COPY Services/Common/. ./Services/Common/
COPY Services/Recommendations/. ./Services/Recommendations/

RUN dotnet publish Services/Recommendations/Recommendations.csproj -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "Recommendations.dll"]
