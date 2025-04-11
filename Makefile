build:
	dotnet build

run:
	dotnet run

publish:
	dotnet publish -c Release -o ./publish

test:
	dotnet test
