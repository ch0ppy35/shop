# NATS Shop

A modern microservice-based webshop application built with .NET, Blazor WebAssembly, and NATS messaging.

## Architecture Overview

NATS Shop is designed as a microservice architecture with the following components:

- **Frontend**: A Blazor WebAssembly application that provides the user interface
- **API Gateway**: An ASP.NET Core Web API that handles HTTP requests and publishes messages to NATS
- **Products Service**: A .NET console application that consumes product-related messages from NATS and manages product data including inventory
- **NATS**: The messaging system that enables communication between services
- **PostgreSQL**: The database for storing product and inventory data

## Architecture Details

### Frontend (Blazor WebAssembly)

The frontend is a single-page application built with Blazor WebAssembly that runs entirely in the browser:

- **Technology**: .NET 9.0, Blazor WebAssembly
- **Deployment**: Containerized with a lightweight Go webserver
- **Features**:
  - Responsive product catalog with pagination
  - Product details view
  - Session tracking across page loads
  - Configurable API endpoints for different environments

### API Gateway

The Gateway service is an ASP.NET Core Web API that acts as the entry point for all client requests:

- **Technology**: ASP.NET Core 9.0
- **Responsibilities**:
  - Exposes RESTful endpoints for products and inventory
  - Publishes messages to NATS for processing by other services
  - Implements request/reply pattern with NATS
  - Provides health and readiness endpoints
  - Manages session IDs for request tracking
  - Handles CORS for browser requests

### Products Service

The Products service is a .NET console application that manages product data and inventory:

- **Technology**: .NET 9.0, Entity Framework Core
- **Responsibilities**:
  - Consumes product-related messages from NATS
  - Manages product data including inventory information
  - Processes product operations (create, update, delete, get)
  - Manages inventory levels for products
  - Handles database migrations and seeding
  - Preserves session IDs in responses

### Common Library

The Common library provides shared functionality for all backend services:

- **Technology**: .NET 9.0
- **Features**:
  - Structured JSON logging
  - NATS connection management with retry logic
  - Health checking endpoints
  - Common message models and DTOs
  - Database access with Entity Framework Core
  - Database migrations
  - Shared service extensions

## Communication Flow

```
┌─────────────┐      HTTP      ┌─────────────┐     NATS     ┌─────────────┐
│   Frontend  │ ─────────────> │   Gateway   │ ───────────> │  Products   │
│  (Browser)  │ <───────────── │    (API)    │ <─────────── │  Service    │
└─────────────┘                └─────────────┘              └──────┬──────┘
                                                                   │
                                                                   │
                                                            ┌──────▼──────┐
                                                            │  PostgreSQL  │
                                                            │  Database    │
                                                            └─────────────┘
```

The communication flow works as follows:

1. **User Interaction**: The user interacts with the Blazor WebAssembly frontend
2. **API Request**: The frontend makes HTTP requests to the Gateway API
3. **Session Tracking**: Each request includes a session ID for tracking
4. **Message Publishing**: The Gateway publishes messages to NATS
5. **Service Processing**: The Products service consumes messages and processes them
6. **Database Operations**: The Products service performs database operations
7. **Response**: Results are sent back through NATS to the Gateway and then to the frontend

## Running the Application

### Prerequisites

- Docker and Docker Compose
- .NET 9.0 SDK (for development)

### Using Docker Compose

The easiest way to run the entire application stack is with Docker Compose:

```bash
# Start all services
docker-compose up
```

This will start:
- The frontend on http://localhost:8081
- The Gateway API on http://localhost:8080
- The Products service
- NATS messaging system
- PostgreSQL database

The Products service will automatically run database migrations and seed the database with sample data.

### Development Environment

To run the services locally for development:

1. Start PostgreSQL and NATS:

```bash
docker run -p 5432:5432 -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres postgres:16
docker run -p 4222:4222 -p 8222:8222 nats:latest
```

2. Create the database:

```bash
psql -h localhost -U postgres -c "CREATE DATABASE products;"
```

3. Run the Products service (this will apply migrations):

```bash
cd Services/Products
dotnet run
```

4. Run the Gateway service:

```bash
cd Services/Gateway
dotnet run
```

5. Run the Frontend application:

```bash
cd App
dotnet run
```

## Technical Features

### Frontend Application Structure

The Blazor WebAssembly frontend is organized as follows:

- **Pages**: Contains the main application pages
  - `Home.razor`: Landing page
  - `Products.razor`: Product catalog with pagination
  - `ProductDetails.razor`: Detailed view of a single product
  - `About.razor`: Information about the application
- **Layout**: Contains layout components
  - `MainLayout.razor`: Main application layout
  - `NavMenu.razor`: Navigation menu
  - `SessionInfo.razor`: Displays the current session ID
- **Services**: Contains service classes for API communication
  - `ProductService.cs`: Handles product API requests
  - `SessionService.cs`: Manages session IDs
  - `ConfigurationService.cs`: Handles application configuration
- **Models**: Contains data models
  - `Product.cs`: Product data model
  - `PaginatedList.cs`: Pagination support

### Database Management

The application uses Entity Framework Core to manage database operations:

- **Migrations**: Automatically applied when the Products service starts
- **Seeding**: Sample product data is loaded on first run
- **Entity Framework Core**: Used for database access and modeling
- **PostgreSQL**: Used as the database engine

To add a new migration during development:

```bash
cd Services/Products
dotnet ef migrations add <MigrationName>
```

### Session Management

The application implements cross-service session tracking:

- **Browser Storage**: Frontend stores session IDs in local storage
- **Request Headers**: Session IDs are included in all API requests via the `X-Session-ID` header
- **Middleware**: Gateway adds session IDs to requests if not present
- **Message Propagation**: Session IDs are included in NATS messages
- **Response Headers**: Session IDs are returned in response headers

### Messaging with NATS

NATS is used for service-to-service communication:

- **Request/Reply Pattern**: Used for synchronous operations
- **Message Serialization**: JSON serialization for messages
- **Connection Resilience**: Automatic reconnection and retry logic
- **Message Routing**: Subject-based routing for different operations

### API Endpoints

#### Health and Readiness

- `GET http://localhost:8080/healthz`: Health check endpoint
- `GET http://localhost:8080/readinessz`: Readiness check endpoint

#### Products

- `GET http://localhost:8080/api/products`: Get paginated products
- `GET http://localhost:8080/api/products/{id}`: Get a product by ID
- `POST http://localhost:8080/api/products`: Create a new product
- `PUT http://localhost:8080/api/products/{id}`: Update a product
- `DELETE http://localhost:8080/api/products/{id}`: Delete a product

#### Inventory

- `GET http://localhost:8080/api/products/{id}/inventory`: Get inventory status
- `PUT http://localhost:8080/api/products/{id}/inventory`: Update inventory

## Configuration

### Environment Variables

#### Gateway Service
- `NATS_URL`: The URL of the NATS server (default: `nats://localhost:4222`)
- `ASPNETCORE_URLS`: The URLs to listen on (default: `http://0.0.0.0:8080`)
- `ASPNETCORE_ENVIRONMENT`: The environment (Development, Staging, Production)

#### Products Service
- `NATS_URL`: The URL of the NATS server (default: `nats://localhost:4222`)
- `DB_CONNECTION_STRING`: The PostgreSQL connection string (default: `Host=localhost;Database=products;Username=postgres;Password=postgres`)

#### Frontend
- `ApiBaseUrl`: Configured in appsettings.json or can be overridden with JavaScript

## Example API Calls

```bash
# Health check
curl http://localhost:8080/healthz

# Get products with pagination
curl http://localhost:8080/api/products?page=1&pageSize=5

# Get a specific product
curl http://localhost:8080/api/products/prod-001

# Create a product
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Product","description":"A test product","price":29.99,"quantity":10,"sku":"SKU123","location":"Warehouse A","quantityInStock":50,"reorderThreshold":10}'

# Using a session ID
curl -X GET http://localhost:8080/api/products \
  -H "X-Session-ID: my-custom-session-id"
```

## Testing

The application includes comprehensive test suites for each component:

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Tests/Common.Tests/Common.Tests.csproj
dotnet test Tests/Products.Tests/Products.Tests.csproj
dotnet test Tests/App.Tests/App.Tests.csproj
```

### Test Projects

- **Common.Tests**: Tests for the Common library
  - Database utilities
  - Logging functionality
  - Messaging components
  - Health checks
  - Model serialization

- **Products.Tests**: Tests for the Products service
  - Repository operations
  - Service methods
  - Message handling
  - Database operations

- **App.Tests**: Tests for the frontend application
  - Component rendering
  - Service functionality
  - Session management
  - Navigation

The tests use xUnit as the test framework, Moq for mocking, and bUnit for Blazor component testing.
