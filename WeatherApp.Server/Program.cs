using MongoDB.Driver;
using WeatherApp.Server.Services;
using WeatherApp.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Alert Service
builder.Services.AddSingleton<IAlertService, AlertService>();

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Register Background Service
builder.Services.AddHostedService<FavoriteWeatherMonitorService>();

// MongoDB Configuration
var mongoConnectionString =
    Environment.GetEnvironmentVariable("MongoDB__ConnectionString")
    ?? builder.Configuration["MongoDB__ConnectionString"]
    ?? builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new Exception("MongoDB connection string not found");

var mongoDatabaseName =
    Environment.GetEnvironmentVariable("MongoDB__DatabaseName")
    ?? builder.Configuration["MongoDB__DatabaseName"]
    ?? "WeatherAppDB";

Console.WriteLine($"DEBUG - MongoDB Connection: {(mongoConnectionString.StartsWith("mongodb+srv") ? "Atlas" : "Local")}");
Console.WriteLine($"DEBUG - MongoDB Database: {mongoDatabaseName}");

builder.Services.AddSingleton<IMongoClient>(_ =>
{
    var settings = MongoClientSettings.FromConnectionString(mongoConnectionString);
    settings.ServerApi = new ServerApi(ServerApiVersion.V1);
    settings.RetryWrites = true;
    settings.RetryReads = true;
    return new MongoClient(settings);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
});

// Supabase Configuration
var supabaseUrl = 
    Environment.GetEnvironmentVariable("Supabase__Url")
    ?? builder.Configuration["Supabase__Url"]
    ?? builder.Configuration["Supabase:Url"] 
    ?? throw new Exception("Supabase Url missing");

var supabaseKey = 
    Environment.GetEnvironmentVariable("Supabase__AnonKey")
    ?? builder.Configuration["Supabase__AnonKey"]
    ?? builder.Configuration["Supabase:AnonKey"] 
    ?? throw new Exception("Supabase AnonKey missing");

builder.Services.AddSingleton(_ =>
{
    var options = new SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = false
    };
    return new Supabase.Client(supabaseUrl, supabaseKey, options);
});

// Register Application Services
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<SupabaseAuthService>();

// JWT Authentication
var jwtSecretKey = 
    Environment.GetEnvironmentVariable("Jwt__SecretKey")
    ?? builder.Configuration["Jwt__SecretKey"]
    ?? builder.Configuration["Jwt:SecretKey"] 
    ?? "your-default-secret-key-min-32-chars-long";

var jwtIssuer = 
    Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? builder.Configuration["Jwt__Issuer"]
    ?? builder.Configuration["Jwt:Issuer"] 
    ?? "WeatherApp";

var jwtAudience = 
    Environment.GetEnvironmentVariable("Jwt__Audience")
    ?? builder.Configuration["Jwt__Audience"]
    ?? builder.Configuration["Jwt:Audience"] 
    ?? "WeatherAppUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// BUILD THE APP (only once!)
var app = builder.Build();

// Startup Logs
Console.WriteLine("===========================================");
Console.WriteLine($"Weather App API - {app.Environment.EnvironmentName}");
Console.WriteLine($"MongoDB: {mongoDatabaseName}");
Console.WriteLine($"Supabase: {supabaseUrl}");
Console.WriteLine("===========================================");

// Test MongoDB Connection
try
{
    Console.WriteLine("Testing MongoDB Atlas connection...");
    Console.WriteLine($"Connection String: {mongoConnectionString}");
    await MongoConnectionTest.TestConnection(mongoConnectionString);
}
catch (Exception ex)
{
    Console.WriteLine($"❌ MongoDB test failed: {ex.Message}");
}

// ✅ Serve Blazor WebAssembly static files (FIRST - before routing)
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather App API v1");
    c.RoutePrefix = "swagger";
});

// Configure Middleware Pipeline
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// API Health Check Endpoint
app.MapGet("/api", () => Results.Ok(new 
{ 
    message = "Weather App API is running!",
    version = "1.0",
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Debug: List all registered endpoints
Console.WriteLine("==================== REGISTERED ENDPOINTS ====================");
var endpoints = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints;
foreach (var endpoint in endpoints)
{
    Console.WriteLine($"  {endpoint.DisplayName}");
}
Console.WriteLine("==============================================================");

// ✅ Fallback to index.html for client-side routing (MUST BE LAST!)
app.MapFallbackToFile("index.html");

app.Run();
