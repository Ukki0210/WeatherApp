using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WeatherApp.Client;
using WeatherApp.Client.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ✅ Read backend API URL from configuration (appsettings.json)
// This allows different URLs for development vs production
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://weatherapp-6i2i.onrender.com";

Console.WriteLine($"Connecting to API: {apiBaseUrl}");

// ✅ Configure default HttpClient to point to backend API
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiBaseUrl)
});

// ✅ Configure HttpClient for AIWeatherService (using same backend)
builder.Services.AddHttpClient<AIWeatherService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// ✅ Register AIWeatherService
builder.Services.AddScoped<AIWeatherService>();

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WeatherApiService>();
builder.Services.AddScoped<UserProfileApiService>();

var host = builder.Build();

// Initialize AuthService
var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

await host.RunAsync();