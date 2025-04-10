FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy the solution file and project files first to optimize Docker cache
COPY nats-shop.sln .
COPY Services/Common/Common.csproj ./Services/Common/
COPY Services/Products/Products.csproj ./Services/Products/

# Restore as distinct layers
RUN dotnet restore Services/Products/Products.csproj

# Copy everything else
COPY Services/Common/. ./Services/Common/
COPY Services/Products/. ./Services/Products/

# Build and publish a release
RUN dotnet publish Services/Products/Products.csproj -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Products.dll"]
