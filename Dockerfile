# ==========================================
# Dockerfile for WeatherApp.Client (Frontend Only)
# ==========================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file if exists (optional, helps with restore)
COPY ["WeatherApp.sln", "./"]

# Copy project files for restore
COPY ["WeatherApp.Client/WeatherApp.Client.csproj", "WeatherApp.Client/"]
COPY ["WeatherApp.Shared/WeatherApp.Shared.csproj", "WeatherApp.Shared/"]

# Restore dependencies
RUN dotnet restore "WeatherApp.Client/WeatherApp.Client.csproj"

# Copy all source code
COPY . .

# Build and publish the CLIENT project (not Server!)
WORKDIR "/src/WeatherApp.Client"
RUN dotnet publish "WeatherApp.Client.csproj" -c Release -o /app/publish

# Verify the publish output
RUN ls -la /app/publish
RUN ls -la /app/publish/wwwroot

# ==========================================
# Runtime stage - Nginx to serve static files
# ==========================================
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Remove default nginx static assets
RUN rm -rf ./*

# Copy published Blazor WASM files from build stage
COPY --from=build /app/publish/wwwroot .

# Verify files were copied
RUN ls -la /usr/share/nginx/html

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Expose port 8080 (Render uses this port)
EXPOSE 8080

# Start nginx
CMD ["nginx", "-g", "daemon off;"]