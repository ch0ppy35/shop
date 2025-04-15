# NATS Shop Frontend

This is the frontend web application for the NATS Shop microservices demo. It's built using Blazor WebAssembly and
communicates with the backend services through the Gateway API.

## Development

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose (for running with the backend services)

### Running Locally

To run the frontend locally during development:

```bash
cd App
dotnet run
```

This will start the application on `http://localhost:5000` by default.

### Building the Docker Image

To build the Docker image:

```bash
cd App
docker build -t nats-shop-frontend .
```

### Running with Docker Compose

To run the entire application stack including the frontend and all backend services:

```bash
# From the root directory of the project
docker-compose up -d
```

This will start the frontend on port 80, and the Gateway API on port 8080.

## Configuration

The application uses the following configuration files:

- `wwwroot/appsettings.json` - Default configuration
- `wwwroot/appsettings.Production.json` - Production-specific configuration

The main configuration settings are:

- `ApiBaseUrl` - The base URL of the Gateway API
- `ConnectionRetryCount` - Number of retries for API connections
- `ConnectionRetryDelaySeconds` - Delay between retries

## Architecture

The frontend is a Blazor WebAssembly application that runs entirely in the browser. It communicates with the backend
services through the Gateway API, which acts as a facade for all the microservices.

When deployed with Docker, the application is built and then served using Nginx as a static file server.
