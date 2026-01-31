using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WeatherApp.Client;
using WeatherApp.Client.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 🔥 IMPORTANT:
// For hosted Blazor (Client + Server together),
// always use the SAME base address the app was loaded from.
// This works locally AND on Render.
builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

// HttpClient for AIWeatherService
builder.Services.AddHttpClient<AIWeatherService>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
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
