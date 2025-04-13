build:
	dotnet build

run:
	dotnet run

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
