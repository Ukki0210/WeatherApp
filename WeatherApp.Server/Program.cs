using MongoDB.Driver;
using WeatherApp.Server.Services;
using WeatherApp.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Supabase;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoConnectionString =
    builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new Exception("MongoDB connection string not found");

var mongoDatabaseName =
    builder.Configuration["MongoDatabase:DatabaseName"] ?? "WeatherAppDB";

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
var supabaseUrl = builder.Configuration["Supabase:Url"] 
    ?? throw new Exception("Supabase Url missing");

var supabaseKey = builder.Configuration["Supabase:AnonKey"] 
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


builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<SupabaseAuthService>();


var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "";

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
        IssuerSigningKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:5159")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddHttpClient();
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();              

app.UseCors("AllowBlazorClient"); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "Weather App API is running!");

Console.WriteLine("===========================================");
Console.WriteLine("Weather App API Started Successfully!");
Console.WriteLine($"MongoDB: {mongoConnectionString}");
Console.WriteLine($"Supabase: {supabaseUrl}");
Console.WriteLine("===========================================");

await MongoConnectionTest.TestConnection(mongoConnectionString);

app.Run();
