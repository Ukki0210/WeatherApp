using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WeatherApp.Server.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ FIXED CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)  // Allow any origin
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// MongoDB Configuration
var mongoConnectionString = 
    Environment.GetEnvironmentVariable("MongoDB__ConnectionString")
    ?? builder.Configuration["MongoDB__ConnectionString"]
    ?? builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new Exception("MongoDB connection string not configured");

var mongoDatabaseName =
    Environment.GetEnvironmentVariable("MongoDB__DatabaseName")
    ?? builder.Configuration["MongoDB__DatabaseName"]
    ?? "WeatherAppDB";

Console.WriteLine($"MongoDB Database: {mongoDatabaseName}");

builder.Services.AddSingleton<IMongoClient>(sp =>
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

// Supabase Configuration
var supabaseUrl = 
    Environment.GetEnvironmentVariable("Supabase__Url")
    ?? builder.Configuration["Supabase__Url"]
    ?? throw new Exception("Supabase URL not configured");

var supabaseKey = 
    Environment.GetEnvironmentVariable("Supabase__AnonKey")
    ?? builder.Configuration["Supabase__AnonKey"]
    ?? throw new Exception("Supabase Key not configured");

builder.Services.AddSingleton(_ =>
{
    var options = new Supabase.SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = false
    };
    return new Supabase.Client(supabaseUrl, supabaseKey, options);
});

// Register services
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IAlertService, AlertService>();
builder.Services.AddHostedService<FavoriteWeatherMonitorService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<SupabaseAuthService>();

// JWT Authentication
var jwtSecretKey = 
    Environment.GetEnvironmentVariable("Jwt__SecretKey")
    ?? builder.Configuration["Jwt__SecretKey"]
    ?? "your-default-secret-key-min-32-chars-long";

var jwtIssuer = 
    Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? builder.Configuration["Jwt__Issuer"]
    ?? "WeatherApp";

var jwtAudience = 
    Environment.GetEnvironmentVariable("Jwt__Audience")
    ?? builder.Configuration["Jwt__Audience"]
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

builder.Services.AddAuthorization();

var app = builder.Build();

Console.WriteLine("===========================================");
Console.WriteLine($"Weather App API - {app.Environment.EnvironmentName}");
Console.WriteLine($"MongoDB: {mongoDatabaseName}");
Console.WriteLine("===========================================");

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather App API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// ✅ CRITICAL: CORS must come after UseRouting() and before UseAuthentication()
app.UseRouting();
app.UseCors("AllowAll");  // ✅ Changed to "AllowAll"
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ✅ ADD HEALTH CHECK ENDPOINTS
app.MapGet("/api", () => Results.Ok(new 
{ 
    message = "Weather App API is running!",
    version = "1.0",
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();