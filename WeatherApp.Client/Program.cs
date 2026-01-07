using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WeatherApp.Client;
using WeatherApp.Client.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<AIWeatherService>();
builder.Services.AddHttpClient<AIWeatherService>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});


// Configure HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("http://localhost:5000/") 
});
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