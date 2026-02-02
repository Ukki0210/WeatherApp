using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WeatherApp.Client;
using WeatherApp.Client.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ✅ FIX: Configure API base URL for separate deployment
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] 
    ?? "https://weatherapp-backend-ov4w.onrender.com"; // Your actual backend URL

Console.WriteLine($"🔥 API Base URL: {apiBaseUrl}"); // Debug log

// Main HttpClient pointing to the backend API
builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(apiBaseUrl)
    });

// HttpClient for AIWeatherService
builder.Services.AddHttpClient<AIWeatherService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// HttpClient for WeatherApiService
builder.Services.AddHttpClient<WeatherApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// HttpClient for UserProfileApiService
builder.Services.AddHttpClient<UserProfileApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Register application services
builder.Services.AddScoped<AIWeatherService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WeatherApiService>();
builder.Services.AddScoped<UserProfileApiService>();

// Local storage
builder.Services.AddBlazoredLocalStorage();

var host = builder.Build();

// Initialize auth state
var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

await host.RunAsync();
