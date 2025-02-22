# Use stable .NET 8 SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY ["API/API.csproj", "./"]
RUN dotnet restore "API.csproj"

# Copy everything and build
COPY . .
RUN dotnet build --no-restore -c Release -o /app/build
RUN dotnet publish --no-build -c Release -o /app/publish

# Use .NET 8 runtime to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
