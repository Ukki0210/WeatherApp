using MongoDB.Driver;
using WeatherApp.Server.Services;
using WeatherApp.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// Configuration
// =====================================================
builder.Configuration.AddEnvironmentVariables();

// =====================================================
// Services
// =====================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Alert & Email
builder.Services.AddSingleton<IAlertService, AlertService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Background Service
builder.Services.AddHostedService<FavoriteWeatherMonitorService>();

// =====================================================
// MongoDB
// =====================================================
var mongoConnectionString =
    Environment.GetEnvironmentVariable("MongoDB__ConnectionString")
    ?? builder.Configuration["MongoDB__ConnectionString"]
    ?? builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new Exception("MongoDB connection string not found");

var mongoDatabaseName =
    Environment.GetEnvironmentVariable("MongoDB__DatabaseName")
    ?? builder.Configuration["MongoDB__DatabaseName"]
    ?? "WeatherAppDB";

builder.Services.AddSingleton<IMongoClient>(_ =>
{
    var settings = MongoClientSettings.FromConnectionString(mongoConnectionString);
    settings.ServerApi = new ServerApi(ServerApiVersion.V1);
    return new MongoClient(settings);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
});

// =====================================================
// Supabase
// =====================================================
var supabaseUrl =
    Environment.GetEnvironmentVariable("Supabase__Url")
    ?? builder.Configuration["Supabase:Url"]
    ?? throw new Exception("Supabase Url missing");

var supabaseKey =
    Environment.GetEnvironmentVariable("Supabase__AnonKey")
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

// =====================================================
// Application Services
// =====================================================
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<SupabaseAuthService>();

// =====================================================
// JWT Authentication
// =====================================================
var jwtSecretKey =
    Environment.GetEnvironmentVariable("Jwt__SecretKey")
    ?? builder.Configuration["Jwt:SecretKey"]
    ?? throw new Exception("JWT SecretKey missing");

var jwtIssuer =
    Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "WeatherApp";

var jwtAudience =
    Environment.GetEnvironmentVariable("Jwt__Audience")
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecretKey)
            )
        };
    });

// =====================================================
// Build App
// =====================================================
var app = builder.Build();

// =====================================================
// Startup Logs
// =====================================================
Console.WriteLine("===========================================");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"MongoDB: {mongoDatabaseName}");
Console.WriteLine($"Supabase: {supabaseUrl}");
Console.WriteLine("===========================================");

// =====================================================
// Middleware Pipeline (ORDER MATTERS)
// =====================================================

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Static files (for swagger / wwwroot assets only)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map API controllers
app.MapControllers();

// Health & debug endpoints
app.MapGet("/api", () => Results.Ok(new
{
    message = "Weather App API is running",
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();

