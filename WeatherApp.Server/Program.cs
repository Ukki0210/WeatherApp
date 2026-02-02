using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WeatherApp.Server.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ CORS Configuration - Allow your frontend domain
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Get frontend URL from environment variable or configuration
        var frontendUrl = builder.Configuration["Frontend:Url"] 
            ?? "https://weatherapp-6i2i.onrender.com"; // Your actual frontend URL
        
        policy.WithOrigins(
                frontendUrl,
                "http://localhost:5000", // For local development
                "https://localhost:5001"  // For local development
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// MongoDB Configuration
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new Exception("MongoDB connection string not configured");

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    return new MongoClient(mongoConnectionString);
});

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["MongoDatabase:DatabaseName"] ?? "WeatherAppDB";
    return client.GetDatabase(databaseName);
});

// Register services
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<EmailService>();
//builder.Services.AddScoped<WeatherAlert>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<FavoriteWeatherMonitorService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<SupabaseAuthService>();

// JWT Authentication
var jwtKey = builder.Configuration["Supabase:AnonKey"]
    ?? throw new Exception("JWT key not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Supabase:Url"],
            ValidAudience = "authenticated",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // ❌ REMOVED: UseWebAssemblyDebugging() - Not needed for separate deployments
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ✅ IMPORTANT: Enable CORS before routing
app.UseCors("AllowFrontend");

// ❌ REMOVED: UseStaticFiles and UseBlazorFrameworkFiles - Not needed for API-only backend
// app.UseStaticFiles();
// app.UseBlazorFrameworkFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ❌ REMOVED: MapFallbackToFile - Not needed for API-only backend
// app.MapFallbackToFile("index.html");

app.Run();
