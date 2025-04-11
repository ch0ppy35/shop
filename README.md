# NATS Shop

A microservice-based webshop using NATS for messaging.

## Architecture

The application consists of the following components:

- **Gateway**: An ASP.NET Core Web API that handles HTTP requests and publishes messages to NATS.
- **Products Service**: A .NET console application that consumes product-related messages from NATS and manages product data including inventory.
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
- Manages product data including inventory information
- Processes product operations (create, update, delete, get)
- Manages inventory levels for products
- Processes inventory-related operations
- Provides real-time inventory status updates

## Common Library

The Common library provides shared functionality for all services:

- JSON logging
- NATS connection management
- Health checking
- Common message models
- Database access with Entity Framework Core
- Database migrations with EF Core

## Running the Application

### Prerequisites

- Docker and Docker Compose
- .NET 9.0 SDK (for development)

### Using Docker Compose

```bash
# Start all services
docker-compose up

# Wait for services to start and migrations to complete, then in a new terminal:
./docker-seed.sh
```

This will seed the database with sample product data after the services have started and migrations have completed.

### Development

To run the services locally:

1. Start PostgreSQL and NATS:

```bash
docker run -p 5432:5432 -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres postgres:16
docker run -p 4222:4222 -p 8222:8222 nats:latest
```

2. Create the database and seed it with sample data:

```bash
psql -h localhost -U postgres -c "CREATE DATABASE products;"

# After running the Products service to apply migrations, seed the database
./seed-database.sh
```

3. Run the Gateway service:

```bash
cd Services/Gateway
dotnet run
```

4. Run the Products service:

```bash
cd Services/Products
dotnet run
```

### Database Migrations

The application uses Entity Framework Core to manage database migrations. Migrations are automatically applied when the services start. To add a new migration:

1. Navigate to the service directory
2. Run `dotnet ef migrations add <MigrationName>`
3. Restart the service to apply the migration

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

- `GET http://localhost:8080/api/products/{id}/inventory`: Get inventory status for a specific product
- `PUT http://localhost:8080/api/products/{id}/inventory`: Update inventory for a product

## Environment Variables

- `NATS_URL`: The URL of the NATS server (default: `nats://localhost:4222`)
- `ASPNETCORE_URLS`: The URLs to listen on (default: `http://0.0.0.0:8080`)
- `ASPNETCORE_ENVIRONMENT`: The environment (Development, Staging, Production)
- `DB_CONNECTION_STRING`: The PostgreSQL connection string (default: `Host=localhost;Database=products;Username=postgres;Password=postgres`)

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
  -d '{"name":"Test Product","description":"A test product","price":29.99,"quantity":10,"sku":"SKU123","location":"Warehouse A","quantityInStock":50,"reorderThreshold":10}'

# Get all products
curl http://localhost:8080/api/products

# Get inventory status for a product
curl http://localhost:8080/api/products/prod-001/inventory

# Update inventory for a product
curl -X PUT http://localhost:8080/api/products/prod-001/inventory \
  -H "Content-Type: application/json" \
  -d '{"sku":"KB-ERG-001","location":"Warehouse A","quantityInStock":60,"reorderThreshold":15}'

# Using a session ID
curl -X GET http://localhost:8080/api/products \
  -H "X-Session-ID: my-custom-session-id"
```
