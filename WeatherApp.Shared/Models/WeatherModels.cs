using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WeatherApp.Shared.Models;
namespace WeatherApp.Shared.Models
{
    // Current Weather Data
    public class WeatherData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public int Pressure { get; set; }
        public int Visibility { get; set; }
        public int Clouds { get; set; }
        public string Icon { get; set; } = string.Empty;
        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }
    }

    // 5-Day Forecast
    public class ForecastData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public List<ForecastItem> Forecasts { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ForecastItem
    {
        public DateTime DateTime { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public int Clouds { get; set; }
        public string Icon { get; set; } = string.Empty;
        public double RainVolume { get; set; }
    }

    // User Model
    public class UserProfile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string SupabaseUserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<string> FavoriteCities { get; set; } = new();
        public string DefaultCity { get; set; } = string.Empty;
        public bool DarkMode { get; set; } = false;
        public string TemperatureUnit { get; set; } = "celsius";
        public List<WeatherAlert> Alerts { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    }

    // Weather Alerts
    public class WeatherAlert
    {
        public string City { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public bool EmailNotification { get; set; } = true;
        public bool PushNotification { get; set; } = false;
    }

    // Historical Alert
    public class AlertHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }

    // Weather Analytics
    public class WeatherAnalytics
    {
        public string City { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double AvgTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public double MinTemperature { get; set; }
        public double AvgHumidity { get; set; }
        public double TotalRainfall { get; set; }
        public List<DailyWeatherSummary> DailySummaries { get; set; } = new();
    }

    public class DailyWeatherSummary
    {
        public DateTime Date { get; set; }
        public double AvgTemp { get; set; }
        public double MaxTemp { get; set; }
        public double MinTemp { get; set; }
        public double Rainfall { get; set; }
        public int Humidity { get; set; }
    }

    // API Request/Response Models
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Top Cities
    public class TopCity
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
    // Add this at the end of WeatherModels.cs (before the closing brace)

    // Active Weather Alert System (for real-time warnings)
    public class ActiveWeatherAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}

