FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY  Frontend.csproj .
RUN dotnet restore
COPY . .
RUN dotnet build "Frontend.csproj" -c Release -o /app/build
RUN dotnet publish "Frontend.csproj" -c Release -o /app/publish

FROM golang:1.24-alpine AS webserver-builder

WORKDIR /app
COPY Scripts/webserver/webserver.go .
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-w -s" -o webserver webserver.go

FROM scratch AS dist

WORKDIR /app
COPY --from=webserver-builder /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/
COPY --from=webserver-builder /app/webserver /app/webserver
COPY --from=build /app/publish/wwwroot /app/wwwroot

ENV ASPNETCORE_ENVIRONMENT=Production
ENV SERVE_DIR=/app/wwwroot
ENV SERVER_PORT=80
EXPOSE 80

CMD ["/app/webserver"]
