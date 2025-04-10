# NATS Shop

A microservice-based webshop using NATS for messaging.

## Architecture

The application consists of the following components:

- **Gateway**: An ASP.NET Core Web API that handles HTTP requests and publishes messages to NATS.
- **Products Service**: A .NET console application that consumes product-related messages from NATS.
- **NATS**: The messaging system that enables communication between services.

## Services

### Gateway

The Gateway service is an ASP.NET Core Web API that:

- Exposes RESTful endpoints for products and orders
- Publishes messages to NATS for processing by other services
- Provides health and readiness endpoints

### Products Service

The Products service is a .NET console application that:

- Consumes product-related messages from NATS
- Manages product data
- Processes product operations (create, update, delete, get)

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

### Orders

- `GET http://localhost:8080/api/orders`: Get all orders
- `GET http://localhost:8080/api/orders/{id}`: Get an order by ID
- `POST http://localhost:8080/api/orders`: Create a new order
- `PUT http://localhost:8080/api/orders/{id}`: Update an order
- `DELETE http://localhost:8080/api/orders/{id}`: Delete an order

## Environment Variables

- `NATS_URL`: The URL of the NATS server (default: `nats://localhost:4222`)
- `ASPNETCORE_URLS`: The URLs to listen on (default: `http://0.0.0.0:8080`)
- `ASPNETCORE_ENVIRONMENT`: The environment (Development, Staging, Production)

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
```
