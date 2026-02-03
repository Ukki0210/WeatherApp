using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WeatherApp.Server.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenWeatherMap__ApiKey"] 
                ?? configuration["OpenWeatherMap:ApiKey"]
                ?? throw new Exception("OpenWeatherMap API key not configured");
            _logger = logger;
        }

        public async Task<object?> GetCurrentWeatherAsync(string city)
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenWeatherMap API returned {StatusCode} for city {City}", response.StatusCode, city);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<JsonElement>(content);

                return new
                {
                    city = weatherData.GetProperty("name").GetString(),
                    country = weatherData.GetProperty("sys").GetProperty("country").GetString(),
                    temperature = weatherData.GetProperty("main").GetProperty("temp").GetDouble(),
                    feelsLike = weatherData.GetProperty("main").GetProperty("feels_like").GetDouble(),
                    description = weatherData.GetProperty("weather")[0].GetProperty("description").GetString(),
                    icon = weatherData.GetProperty("weather")[0].GetProperty("icon").GetString(), // ✅ ADDED
                    humidity = weatherData.GetProperty("main").GetProperty("humidity").GetInt32(),
                    pressure = weatherData.GetProperty("main").GetProperty("pressure").GetInt32(),
                    windSpeed = weatherData.GetProperty("wind").GetProperty("speed").GetDouble(),
                    clouds = weatherData.GetProperty("clouds").GetProperty("all").GetInt32(),
                    visibility = weatherData.TryGetProperty("visibility", out var vis) ? vis.GetInt32() : 10000,
                    timestamp = DateTimeOffset.FromUnixTimeSeconds(weatherData.GetProperty("dt").GetInt64()).DateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather for {City}", city);
                throw;
            }
        }

        // ✅ NEW METHOD - Get weather by coordinates
        public async Task<object?> GetWeatherByCoordinatesAsync(double lat, double lon)
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={_apiKey}&units=metric";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenWeatherMap API returned {StatusCode} for coordinates ({Lat}, {Lon})", 
                        response.StatusCode, lat, lon);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<JsonElement>(content);

                return new
                {
                    city = weatherData.GetProperty("name").GetString(), // ✅ This will be the city name like "Bangalore"
                    country = weatherData.GetProperty("sys").GetProperty("country").GetString(),
                    temperature = weatherData.GetProperty("main").GetProperty("temp").GetDouble(),
                    feelsLike = weatherData.GetProperty("main").GetProperty("feels_like").GetDouble(),
                    description = weatherData.GetProperty("weather")[0].GetProperty("description").GetString(),
                    icon = weatherData.GetProperty("weather")[0].GetProperty("icon").GetString(), // ✅ ADDED
                    humidity = weatherData.GetProperty("main").GetProperty("humidity").GetInt32(),
                    pressure = weatherData.GetProperty("main").GetProperty("pressure").GetInt32(),
                    windSpeed = weatherData.GetProperty("wind").GetProperty("speed").GetDouble(),
                    clouds = weatherData.GetProperty("clouds").GetProperty("all").GetInt32(),
                    visibility = weatherData.TryGetProperty("visibility", out var vis) ? vis.GetInt32() : 10000,
                    timestamp = DateTimeOffset.FromUnixTimeSeconds(weatherData.GetProperty("dt").GetInt64()).DateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather for coordinates ({Lat}, {Lon})", lat, lon);
                throw;
            }
        }

        public async Task<WeatherData?> FetchCurrentWeatherAsync(string city, string userId)
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenWeatherMap API returned {StatusCode} for city {City}", response.StatusCode, city);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<JsonElement>(content);

                return new WeatherData
                {
                    City = weatherData.GetProperty("name").GetString() ?? city,
                    Temperature = weatherData.GetProperty("main").GetProperty("temp").GetDouble(),
                    FeelsLike = weatherData.GetProperty("main").GetProperty("feels_like").GetDouble(),
                    Description = weatherData.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                    Icon = weatherData.GetProperty("weather")[0].GetProperty("icon").GetString() ?? "01d", // ✅ ADDED
                    Humidity = weatherData.GetProperty("main").GetProperty("humidity").GetInt32(),
                    WindSpeed = weatherData.GetProperty("wind").GetProperty("speed").GetDouble(),
                    Visibility = weatherData.TryGetProperty("visibility", out var vis) ? vis.GetInt32() : 10000,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather for {City}, User: {UserId}", city, userId);
                return null;
            }
        }

        public async Task<object?> GetForecastAsync(string city)
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&appid={_apiKey}&units=metric";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenWeatherMap forecast API returned {StatusCode} for city {City}", response.StatusCode, city);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var forecastData = JsonSerializer.Deserialize<JsonElement>(content);

                var forecastList = new List<object>();
                
                if (forecastData.TryGetProperty("list", out var list))
                {
                    foreach (var item in list.EnumerateArray())
                    {
                        forecastList.Add(new
                        {
                            dateTime = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).DateTime,
                            temperature = item.GetProperty("main").GetProperty("temp").GetDouble(),
                            feelsLike = item.GetProperty("main").GetProperty("feels_like").GetDouble(),
                            tempMin = item.GetProperty("main").GetProperty("temp_min").GetDouble(),
                            tempMax = item.GetProperty("main").GetProperty("temp_max").GetDouble(),
                            description = item.GetProperty("weather")[0].GetProperty("description").GetString(),
                            icon = item.GetProperty("weather")[0].GetProperty("icon").GetString(), // ✅ ADDED
                            humidity = item.GetProperty("main").GetProperty("humidity").GetInt32(),
                            windSpeed = item.GetProperty("wind").GetProperty("speed").GetDouble(),
                            clouds = item.GetProperty("clouds").GetProperty("all").GetInt32(),
                            pop = item.TryGetProperty("pop", out var popValue) ? popValue.GetDouble() * 100 : 0
                        });
                    }
                }

                return new
                {
                    city = forecastData.GetProperty("city").GetProperty("name").GetString(),
                    country = forecastData.GetProperty("city").GetProperty("country").GetString(),
                    forecasts = forecastList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching forecast for {City}", city);
                throw;
            }
        }
    }

    // ✅ UPDATED - Added Icon property
    public class WeatherData
    {
        public string City { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "01d"; // ✅ ADDED - Weather icon code (e.g., "01d", "10n")
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public int Visibility { get; set; } = 10000;
        public DateTime Timestamp { get; set; }
    }
}