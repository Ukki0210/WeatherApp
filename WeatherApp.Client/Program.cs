using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WeatherApp.Client;
using WeatherApp.Client.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ✅ FIXED: Configure HttpClient to point to your deployed backend
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("https://weatherapp-6i21.onrender.com/") 
});

// ✅ Configure HttpClient for AIWeatherService (using same backend)
builder.Services.AddHttpClient<AIWeatherService>(client =>
{
    client.BaseAddress = new Uri("https://weatherapp-6i21.onrender.com/");
});

// ✅ Register AIWeatherService (only once, not twice)
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
