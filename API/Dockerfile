# Stage 1: Build the .NET 8 app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project file and restore dependencies
COPY ["API/API.csproj", "./"]
RUN dotnet restore "API.csproj"

# Copy everything and build the app
COPY . .
RUN dotnet build --no-restore -c Release -o /app/build
RUN dotnet publish --no-build -c Release -o /app/publish

# Stage 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
