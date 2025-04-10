FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy the solution file and project files first to optimize Docker cache
COPY nats-shop.sln .
COPY Services/Common/Common.csproj ./Services/Common/
COPY Services/Gateway/Gateway.csproj ./Services/Gateway/

# Restore as distinct layers
RUN dotnet restore Services/Gateway/Gateway.csproj

# Copy everything else
COPY Services/Common/. ./Services/Common/
COPY Services/Gateway/. ./Services/Gateway/

# Build and publish a release
RUN dotnet publish Services/Gateway/Gateway.csproj -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "Gateway.dll"]