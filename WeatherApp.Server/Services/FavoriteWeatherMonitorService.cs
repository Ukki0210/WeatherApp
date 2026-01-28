using WeatherApp.Shared.Models;
using Microsoft.Extensions.Hosting;

namespace WeatherApp.Server.Services
{
    public class FavoriteWeatherMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FavoriteWeatherMonitorService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public FavoriteWeatherMonitorService(
            IServiceProvider serviceProvider,
            ILogger<FavoriteWeatherMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Favorite Weather Monitor Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckFavoriteWeatherAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Favorite Weather Monitor");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckFavoriteWeatherAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            
            var userProfileService = scope.ServiceProvider.GetRequiredService<UserProfileService>();
            var weatherService = scope.ServiceProvider.GetRequiredService<WeatherService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            _logger.LogInformation("Checking weather for all users' favorite cities...");

            // Get all users
            var allUsers = await userProfileService.GetAllUsersAsync();

            foreach (var user in allUsers)
            {
                if (user.FavoriteCities == null || !user.FavoriteCities.Any())
                    continue;

                foreach (var city in user.FavoriteCities)
                {
                    try
                    {
                        // Fetch weather
                        var weather = await weatherService.FetchCurrentWeatherAsync(city, user.SupabaseUserId);
                        
                        if (weather == null) continue;

                        // Check for severe conditions using realistic thresholds
                        var (isSevere, alertMessage) = CheckSevereWeatherConditions(weather);

                        if (isSevere)
                        {
                            // Send email alert
                            await emailService.SendWeatherAlertAsync(
                                user.Email,
                                user.DisplayName,
                                city,
                                alertMessage
                            );

                            _logger.LogInformation("Alert email sent to {User} for {City}: {Alert}", 
                                user.Email, city, alertMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking weather for {City}", city);
                    }

                    // Small delay to avoid API rate limits
                    await Task.Delay(1000);
                }
            }

            _logger.LogInformation("Finished checking all favorite cities");
        }

        /// <summary>
        /// Checks for severe weather conditions based on realistic thresholds
        /// </summary>
        /// <summary>
/// Checks for severe weather conditions based on realistic thresholds
/// </summary>
private (bool isSevere, string alertMessage) CheckSevereWeatherConditions(WeatherData weather)
{
    var alerts = new List<string>();

    // 1. EXTREME HEAT: >= 40°C (104°F)
    if (weather.Temperature >= 40)
    {
        alerts.Add($"🔥 EXTREME HEAT WARNING: Temperature has reached {weather.Temperature:F1}°C. Stay hydrated, avoid outdoor activities, and check on elderly neighbors.");
    }

    // 2. FREEZING CONDITIONS: < 0°C (32°F) - UPDATED
    if (weather.Temperature < 0)
    {
        // Determine severity level
        if (weather.Temperature <= -15)
        {
            // Critical: <= -15°C
            alerts.Add($"🚨 CRITICAL COLD ALERT: Temperature has plummeted to {weather.Temperature:F1}°C. Extreme danger of frostbite and hypothermia. Stay indoors!");
        }
        else if (weather.Temperature <= -10)
        {
            // Severe: -10°C to -15°C
            alerts.Add($"❄️ SEVERE COLD WARNING: Temperature has dropped to {weather.Temperature:F1}°C. High risk of frostbite. Limit time outdoors and dress in layers.");
        }
        else
        {
            // Moderate: 0°C to -10°C (including -8°C)
            alerts.Add($"🧊 FREEZING WEATHER ALERT: Temperature is below freezing at {weather.Temperature:F1}°C. Dress warmly and watch for icy conditions.");
        }
    }

    // 3. SEVERE HEAT: >= 35°C (95°F) - Secondary threshold
    else if (weather.Temperature >= 35)
    {
        alerts.Add($"☀️ HEAT ADVISORY: Temperature is {weather.Temperature:F1}°C. Stay cool, drink plenty of water, and avoid strenuous outdoor activities.");
    }

    // 4. HIGH WIND: >= 60 km/h (37 mph)
    if (weather.WindSpeed >= 60)
    {
        alerts.Add($"💨 HIGH WIND WARNING: Wind speeds reaching {weather.WindSpeed:F0} km/h. Secure loose objects, avoid travel if possible.");
    }

    // 5. THUNDERSTORM / SEVERE WEATHER
    if (!string.IsNullOrEmpty(weather.Description) && 
        (weather.Description.Contains("thunderstorm", StringComparison.OrdinalIgnoreCase) ||
         weather.Description.Contains("tornado", StringComparison.OrdinalIgnoreCase) ||
         weather.Description.Contains("hurricane", StringComparison.OrdinalIgnoreCase) ||
         weather.Description.Contains("storm", StringComparison.OrdinalIgnoreCase)))
    {
        alerts.Add($"⚡ SEVERE WEATHER ALERT: {weather.Description} detected. Stay indoors and avoid windows.");
    }

    // 6. SNOW / WINTER STORM
    if (!string.IsNullOrEmpty(weather.Description) && 
        weather.Description.Contains("snow", StringComparison.OrdinalIgnoreCase) && 
        weather.Temperature < 0)
    {
        alerts.Add($"🌨️ WINTER STORM WARNING: Heavy snowfall with temperatures at {weather.Temperature:F1}°C. Travel may be hazardous.");
    }

    // 7. POOR VISIBILITY (if available in your model)
    if (weather.Visibility > 0 && weather.Visibility < 1000)
    {
        alerts.Add($"🌫️ VISIBILITY WARNING: Visibility reduced to {weather.Visibility}m due to fog/dust. Drive with extreme caution.");
    }

    // 8. DANGEROUS HEAT INDEX (High humidity + high temperature)
    if (weather.Humidity >= 90 && weather.Temperature > 30)
    {
        alerts.Add($"🥵 HEAT INDEX ALERT: High humidity ({weather.Humidity}%) combined with {weather.Temperature:F1}°C creates dangerous conditions. Stay in air-conditioned areas.");
    }

    // 9. FREEZING RAIN
    if (!string.IsNullOrEmpty(weather.Description) && 
        weather.Description.Contains("rain", StringComparison.OrdinalIgnoreCase) && 
        weather.Temperature <= 0)
    {
        alerts.Add($"🧊 FREEZING RAIN WARNING: Rain falling with temperatures at {weather.Temperature:F1}°C. Icy conditions expected.");
    }

    // 10. HEAVY RAIN
    if (!string.IsNullOrEmpty(weather.Description) && 
        (weather.Description.Contains("heavy rain", StringComparison.OrdinalIgnoreCase) ||
         weather.Description.Contains("heavy intensity", StringComparison.OrdinalIgnoreCase)))
    {
        alerts.Add($"🌧️ HEAVY RAINFALL ALERT: Heavy rain expected. Flood risk - avoid low-lying areas.");
    }

    if (alerts.Any())
    {
        return (true, string.Join(" | ", alerts));
    }

    return (false, string.Empty);
}
    }
}
