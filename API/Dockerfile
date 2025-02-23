# Use stable .NET 8 SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the API project file and restore dependencies
COPY ["API/API.csproj", "API/"]
RUN dotnet restore "API/API.csproj"

# Copy everything else and build
COPY . . 
RUN dotnet build "API/API.csproj" --no-restore -c Release -o /app/build

# Publish the application
RUN dotnet publish "API/API.csproj" --no-build -c Release -o /app/publish

# Use .NET 8 runtime to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish . 
ENTRYPOINT ["dotnet", "API.dll"]
