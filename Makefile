build:
	dotnet build

publish:
	dotnet publish -c Release -o ./publish

test:
	dotnet test --collect:"XPlat Code Coverage"

test-app:
	dotnet test Tests/App.Tests/App.Tests.csproj

build-app:	
	dotnet publish -c Release -o ./App/publish App

build-app-docker:
	docker build -t nats-shop-frontend:latest ./App

build-gateway-docker:
	docker build -t nats-shop-gateway:latest ./Services/Gateway

build-products-docker:
	docker build -t nats-shop-products:latest ./Services/Products

build-cart-docker:
	docker build -t nats-shop-cart:latest ./Services/Cart

start-gateway-service:
	NATS_URL=nats://localhost:4222 dotnet run --project Services/Gateway/Gateway.csproj

start-cart-service:
	NATS_URL=nats://localhost:4222 REDIS_CONNECTION_STRING=localhost:6379 dotnet run --project Services/Cart/Cart.csproj

start-products-service:
	NATS_URL=nats://localhost:4222 DB_CONNECTION_STRING="Host=localhost;Database=products;Username=postgres;Password=postgres" dotnet run --project Services/Products/Products.csproj
