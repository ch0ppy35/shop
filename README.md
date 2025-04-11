# NATS Shop

A microservice-based webshop using NATS for messaging.

## Architecture

The application consists of the following components:

- **Gateway**: An ASP.NET Core Web API that handles HTTP requests and publishes messages to NATS.
- **Products Service**: A .NET console application that consumes product-related messages from NATS.
- **Inventory Service**: A .NET console application that manages inventory levels and stock availability.
- **NATS**: The messaging system that enables communication between services.

## Services

### Gateway

The Gateway service is an ASP.NET Core Web API that:

- Exposes RESTful endpoints for products and inventory
- Publishes messages to NATS for processing by other services
- Provides health and readiness endpoints

### Products Service

The Products service is a .NET console application that:

- Consumes product-related messages from NATS
- Manages product data
- Processes product operations (create, update, delete, get)

### Inventory Service

The Inventory service is a .NET console application that:

- Manages inventory levels for products
- Processes inventory-related operations (reserve, release, adjust)
- Validates stock availability for product operations
- Provides real-time inventory status updates

## Common Library

The Common library provides shared functionality for all services:

- JSON logging
- NATS connection management
- Health checking
- Common message models

## Running the Application

### Prerequisites

- Docker and Docker Compose
- .NET 9.0 SDK (for development)

### Using Docker Compose

```bash
docker-compose up
```

### Development

To run the services locally:

1. Start NATS:

```bash
docker run -p 4222:4222 -p 8222:8222 nats:latest
```

2. Run the Gateway service:

```bash
cd Services/Gateway
dotnet run
```

3. Run the Products service:

```bash
cd Services/Products
dotnet run
```

4. Run the Inventory service:

```bash
cd Services/Inventory
dotnet run
```

## API Endpoints

### Health and Readiness

- `GET http://localhost:8080/healthz`: Health check endpoint
- `GET http://localhost:8080/readinessz`: Readiness check endpoint

### Products

- `GET http://localhost:8080/api/products`: Get all products
- `GET http://localhost:8080/api/products/{id}`: Get a product by ID
- `POST http://localhost:8080/api/products`: Create a new product
- `PUT http://localhost:8080/api/products/{id}`: Update a product
- `DELETE http://localhost:8080/api/products/{id}`: Delete a product

### Inventory

- `GET http://localhost:8080/api/inventory`: Get inventory status for all products
- `GET http://localhost:8080/api/inventory/{productId}`: Get inventory status for a specific product
- `POST http://localhost:8080/api/inventory/reserve`: Reserve inventory for a product
- `POST http://localhost:8080/api/inventory/release`: Release previously reserved inventory
- `PUT http://localhost:8080/api/inventory/{productId}`: Adjust inventory level for a product

## Environment Variables

- `NATS_URL`: The URL of the NATS server (default: `nats://localhost:4222`)
- `ASPNETCORE_URLS`: The URLs to listen on (default: `http://0.0.0.0:8080`)
- `ASPNETCORE_ENVIRONMENT`: The environment (Development, Staging, Production)

## Session IDs

The application supports session IDs for tracking requests across services:

- Session IDs can be provided in the `X-Session-ID` header
- If no session ID is provided, a new one is generated
- Session IDs are passed to all downstream services
- Session IDs are included in response headers and response bodies

## Example API Calls

```bash
# Health check
curl http://localhost:8080/healthz

# Create a product
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Product","description":"A test product","price":29.99,"quantity":10}'

# Get all products
curl http://localhost:8080/api/products

# Get inventory status for a product
curl http://localhost:8080/api/inventory/2d59cad2-5923-40ab-a09a-58c8d3638a4e

# Reserve inventory
curl -X POST http://localhost:8080/api/inventory/reserve \
  -H "Content-Type: application/json" \
  -d '{"productId":"2d59cad2-5923-40ab-a09a-58c8d3638a4e","quantity":5}'

# Using a session ID
curl -X GET http://localhost:8080/api/products \
  -H "X-Session-ID: my-custom-session-id"
```
