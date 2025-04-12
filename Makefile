build:
	dotnet build

run:
	dotnet run

publish:
	dotnet publish -c Release -o ./publish

test:
	dotnet test --collect:"XPlat Code Coverage"

build-web:	
	dotnet publish -c Release -o ./App/publish
