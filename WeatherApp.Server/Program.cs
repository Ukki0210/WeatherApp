using MongoDB.Driver;
using WeatherApp.Server.Services;
using WeatherApp.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

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
    builder.Configuration["MongoDB__ConnectionString"]
    ?? builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new Exception("MongoDB connection string not found");

var mongoDatabaseName =
    builder.Configuration["MongoDB__DatabaseName"]
    ?? builder.Configuration["MongoDatabase:DatabaseName"] 
    ?? "WeatherAppDB";

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
var supabaseUrl = builder.Configuration["Supabase__Url"]
    ?? builder.Configuration["Supabase:Url"] 
    ?? throw new Exception("Supabase Url missing");

var supabaseKey = builder.Configuration["Supabase__AnonKey"]
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
var jwtSecretKey = builder.Configuration["Jwt__SecretKey"]
    ?? builder.Configuration["Jwt:SecretKey"] 
    ?? "your-default-secret-key-min-32-chars-long";

var jwtIssuer = builder.Configuration["Jwt__Issuer"]
    ?? builder.Configuration["Jwt:Issuer"] 
    ?? "WeatherApp";

var jwtAudience = builder.Configuration["Jwt__Audience"]
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

var app = builder.Build();

// Configure Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// ROOT ENDPOINT - IMPORTANT!
app.MapGet("/", () => Results.Ok(new 
{ 
    message = "Weather App API is running!",
    version = "1.0",
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Startup Logs
Console.WriteLine("===========================================");
Console.WriteLine($"Weather App API - {app.Environment.EnvironmentName}");
Console.WriteLine($"MongoDB: {mongoDatabaseName}");
Console.WriteLine($"Supabase: {supabaseUrl}");
Console.WriteLine("===========================================");

// Test MongoDB Connection
try
{
    await MongoConnectionTest.TestConnection(mongoConnectionString);
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ MongoDB test failed: {ex.Message}");
}

app.Run();
