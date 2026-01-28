using WeatherApp.Shared.Models;
using System.Collections.Concurrent;

namespace WeatherApp.Server.Services
{
    // Remove the ActiveWeatherAlert class definition from here

    public interface IAlertService
    {
        Task<List<ActiveWeatherAlert>> GetActiveAlertsAsync(string location);
        Task<ActiveWeatherAlert> CreateAlertAsync(ActiveWeatherAlert alert);
        Task<bool> DeleteAlertAsync(string alertId);
        Task CheckAndTriggerAlertsAsync(string location, double temperature, double windSpeed, int humidity);
    }

    public class AlertService : IAlertService
    {
        private readonly ConcurrentBag<ActiveWeatherAlert> _alerts = new();
        private readonly ILogger<AlertService> _logger;

        public AlertService(ILogger<AlertService> logger)
        {
            _logger = logger;
        }

        public Task<List<ActiveWeatherAlert>> GetActiveAlertsAsync(string location)
        {
            var activeAlerts = _alerts
                .Where(a => a.Location.Equals(location, StringComparison.OrdinalIgnoreCase) 
                         && a.EndTime > DateTime.UtcNow)
                .OrderByDescending(a => GetSeverityPriority(a.Severity))
                .ToList();
            
            return Task.FromResult(activeAlerts);
        }

        public Task<ActiveWeatherAlert> CreateAlertAsync(ActiveWeatherAlert alert)
        {
            _alerts.Add(alert);
            _logger.LogInformation("Alert created: {Title} for {Location}", alert.Title, alert.Location);
            return Task.FromResult(alert);
        }

        public Task<bool> DeleteAlertAsync(string alertId)
        {
            var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
            return Task.FromResult(alert != null);
        }

        public async Task CheckAndTriggerAlertsAsync(string location, double temperature, double windSpeed, int humidity)
        {
            if (temperature > 10)
            {
                var heatAlert = new ActiveWeatherAlert
                {
                    Title = "Extreme Heat Warning",
                    Description = $"Temperature has reached {temperature}°C. Stay hydrated and avoid outdoor activities.",
                    Severity = "Warning",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddHours(6),
                    Location = location,
                    Type = "Heat"
                };
                await CreateAlertAsync(heatAlert);
            }
            else if (temperature < 5)
            {
                var coldAlert = new ActiveWeatherAlert
                {
                    Title = "Cold Weather Advisory",
                    Description = $"Temperature has dropped to {temperature}°C. Dress warmly.",
                    Severity = "Advisory",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddHours(12),
                    Location = location,
                    Type = "Cold"
                };
                await CreateAlertAsync(coldAlert);
            }

            if (windSpeed > 50)
            {
                var windAlert = new ActiveWeatherAlert
                {
                    Title = "High Wind Warning",
                    Description = $"Wind speeds of {windSpeed} km/h expected. Secure loose objects.",
                    Severity = "Warning",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddHours(4),
                    Location = location,
                    Type = "Wind"
                };
                await CreateAlertAsync(windAlert);
            }

            if (humidity > 90)
            {
                var rainAlert = new ActiveWeatherAlert
                {
                    Title = "Heavy Rain Advisory",
                    Description = "High humidity indicates potential heavy rainfall. Carry an umbrella.",
                    Severity = "Advisory",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddHours(3),
                    Location = location,
                    Type = "Rain"
                };
                await CreateAlertAsync(rainAlert);
            }
        }

        private int GetSeverityPriority(string severity)
        {
            return severity switch
            {
                "Warning" => 3,
                "Watch" => 2,
                "Advisory" => 1,
                _ => 0
            };
        }
    }
}
