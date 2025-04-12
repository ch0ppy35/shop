# Stage 1: Build the Blazor WebAssembly app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Frontend.csproj", "./"]
RUN dotnet restore

# Copy the rest of the files and build
COPY . .
RUN dotnet build "Frontend.csproj" -c Release -o /app/build
RUN dotnet publish "Frontend.csproj" -c Release -o /app/publish

# Stage 2: Create the Nginx container with the published app
FROM nginx:1.27.4 AS dist
WORKDIR /usr/share/nginx/html

# Copy the published app to the nginx html directory
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Set environment to Production
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy custom nginx configuration if needed
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

# Start Nginx
CMD ["nginx", "-g", "daemon off;"]
