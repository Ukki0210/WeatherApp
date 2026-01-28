using MongoDB.Driver;
using WeatherApp.Server.Services;
using WeatherApp.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Supabase;

var builder = WebApplication.CreateBuilder(args);


// ✅ DEBUG: Print ALL environment variables
Console.WriteLine("==================== ENVIRONMENT VARIABLES ====================");
Console.WriteLine($"MONGODB__CONNECTIONSTRING: {Environment.GetEnvironmentVariable("MONGODB__CONNECTIONSTRING")}");
Console.WriteLine($"MONGODB__DATABASENAME: {Environment.GetEnvironmentVariable("MONGODB__DATABASENAME")}");
Console.WriteLine($"SUPABASE__URL: {Environment.GetEnvironmentVariable("SUPABASE__URL")}");
Console.WriteLine($"SUPABASE__KEY: {Environment.GetEnvironmentVariable("SUPABASE__KEY")}");
Console.WriteLine($"OPENWEATHERMAP__APIKEY: {Environment.GetEnvironmentVariable("OPENWEATHERMAP__APIKEY")}");
Console.WriteLine("================================================================");

// ... rest of your code


// ✅ ADD THIS LINE - Force load environment variables
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

// ✅ UPDATED MongoDB Configuration - Read from environment first
var mongoConnectionString =
    Environment.GetEnvironmentVariable("MONGODB__CONNECTIONSTRING")
    ?? builder.Configuration["MONGODB__CONNECTIONSTRING"]
    ?? builder.Configuration["MongoDB__ConnectionString"]
    ?? builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new Exception("MongoDB connection string not found");

var mongoDatabaseName =
    Environment.GetEnvironmentVariable("MONGODB__DATABASENAME")
    ?? builder.Configuration["MONGODB__DATABASENAME"]
    ?? builder.Configuration["MongoDB__DatabaseName"]
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

// ✅ UPDATED Supabase Configuration
var supabaseUrl = 
    Environment.GetEnvironmentVariable("SUPABASE__URL")
    ?? builder.Configuration["SUPABASE__URL"]
    ?? builder.Configuration["Supabase__Url"]
    ?? builder.Configuration["Supabase:Url"] 
    ?? throw new Exception("Supabase Url missing");

var supabaseKey = 
    Environment.GetEnvironmentVariable("SUPABASE__KEY")
    ?? builder.Configuration["SUPABASE__KEY"]
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

// ✅ UPDATED JWT Authentication
var jwtSecretKey = 
    Environment.GetEnvironmentVariable("JWT__SECRETKEY")
    ?? builder.Configuration["JWT__SECRETKEY"]
    ?? builder.Configuration["Jwt__SecretKey"]
    ?? builder.Configuration["Jwt:SecretKey"] 
    ?? "your-default-secret-key-min-32-chars-long";

var jwtIssuer = 
    Environment.GetEnvironmentVariable("JWT__ISSUER")
    ?? builder.Configuration["JWT__ISSUER"]
    ?? builder.Configuration["Jwt__Issuer"]
    ?? builder.Configuration["Jwt:Issuer"] 
    ?? "WeatherApp";

var jwtAudience = 
    Environment.GetEnvironmentVariable("JWT__AUDIENCE")
    ?? builder.Configuration["JWT__AUDIENCE"]
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

var app = builder.Build();

// Configure Middleware Pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
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

// ROOT ENDPOINT
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
    Console.WriteLine("Testing MongoDB Atlas connection...");
    Console.WriteLine($"Connection String: {mongoConnectionString}");
    await MongoConnectionTest.TestConnection(mongoConnectionString);
}
catch (Exception ex)
{
    Console.WriteLine($"❌ MongoDB test failed: {ex.Message}");
}

app.Run();
