# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["WeatherApp.Server/WeatherApp.Server.csproj", "WeatherApp.Server/"]
COPY ["WeatherApp.Client/WeatherApp.Client.csproj", "WeatherApp.Client/"]
COPY ["WeatherApp.Shared/WeatherApp.Shared.csproj", "WeatherApp.Shared/"]

RUN dotnet restore "WeatherApp.Server/WeatherApp.Server.csproj"

# Copy everything else
COPY . .

# Build and publish the server project
WORKDIR "/src/WeatherApp.Server"
RUN dotnet publish "WeatherApp.Server.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Expose port 8080 (Render's default)
EXPOSE 8080

# Set the port environment variable
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "WeatherApp.Server.dll"]
