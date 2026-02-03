using Microsoft.AspNetCore.Mvc;
using WeatherApp.Server.Services;
using WeatherApp.Shared.Models;
using MongoDB.Driver;
using SharedWeatherData = WeatherApp.Shared.Models.WeatherData;

namespace WeatherApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;
        private readonly IMongoDatabase _database;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(
            WeatherService weatherService, 
            IMongoDatabase database,
            ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _database = database;
            _logger = logger;
        }

        // ✅ GET /api/weather/current/{city}
        [HttpGet("current/{city}")]
        public async Task<IActionResult> GetCurrentWeather(string city, [FromQuery] string? userId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new ApiResponse<SharedWeatherData>
                    {
                        Success = false,
                        Message = "City parameter is required"
                    });
                }

                _logger.LogInformation("Fetching weather for: {City}", city);

                var weatherObj = await _weatherService.GetCurrentWeatherAsync(city);
                
                if (weatherObj == null)
                {
                    return NotFound(new ApiResponse<SharedWeatherData>
                    {
                        Success = false,
                        Message = $"Weather not found for city: {city}"
                    });
                }

                // Convert dynamic object to WeatherData
                var weatherData = new SharedWeatherData
                {
                    City = weatherObj.GetType().GetProperty("city")?.GetValue(weatherObj)?.ToString() ?? city,
                    Country = weatherObj.GetType().GetProperty("country")?.GetValue(weatherObj)?.ToString() ?? "",
                    Temperature = Convert.ToDouble(weatherObj.GetType().GetProperty("temperature")?.GetValue(weatherObj) ?? 0),
                    FeelsLike = Convert.ToDouble(weatherObj.GetType().GetProperty("feelsLike")?.GetValue(weatherObj) ?? 0),
                    Description = weatherObj.GetType().GetProperty("description")?.GetValue(weatherObj)?.ToString() ?? "",
                    Humidity = Convert.ToInt32(weatherObj.GetType().GetProperty("humidity")?.GetValue(weatherObj) ?? 0),
                    WindSpeed = Convert.ToDouble(weatherObj.GetType().GetProperty("windSpeed")?.GetValue(weatherObj) ?? 0),
                    Pressure = Convert.ToInt32(weatherObj.GetType().GetProperty("pressure")?.GetValue(weatherObj) ?? 0),
                    Clouds = Convert.ToInt32(weatherObj.GetType().GetProperty("clouds")?.GetValue(weatherObj) ?? 0),
                    Visibility = Convert.ToInt32(weatherObj.GetType().GetProperty("visibility")?.GetValue(weatherObj) ?? 10000),
                    Timestamp = Convert.ToDateTime(weatherObj.GetType().GetProperty("timestamp")?.GetValue(weatherObj) ?? DateTime.UtcNow),
                    UserId = userId
                };

                return Ok(new ApiResponse<SharedWeatherData>
                {
                    Success = true,
                    Data = weatherData,
                    Message = "Weather fetched successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather for {City}", city);
                return StatusCode(500, new ApiResponse<SharedWeatherData>
                {
                    Success = false,
                    Message = "Failed to fetch weather"
                });
            }
        }

        // ✅ GET /api/weather/coordinates?lat=...&lon=...
        // ✅ GET /api/weather/coordinates?lat=...&lon=...
[HttpGet("coordinates")]
public async Task<IActionResult> GetWeatherByCoordinates(
    [FromQuery] double lat, 
    [FromQuery] double lon, 
    [FromQuery] string? userId = null)
{
    try
    {
        _logger.LogInformation("Fetching weather for coordinates: {Lat}, {Lon}", lat, lon);

        var weatherObj = await _weatherService.GetWeatherByCoordinatesAsync(lat, lon);
        
        if (weatherObj == null)
        {
            return NotFound(new ApiResponse<SharedWeatherData>
            {
                Success = false,
                Message = $"Weather not found for coordinates ({lat}, {lon})"
            });
        }

        // Convert dynamic object to WeatherData
        var weatherData = new SharedWeatherData
        {
            City = weatherObj.GetType().GetProperty("city")?.GetValue(weatherObj)?.ToString() ?? "Unknown",
            Country = weatherObj.GetType().GetProperty("country")?.GetValue(weatherObj)?.ToString() ?? "",
            Temperature = Convert.ToDouble(weatherObj.GetType().GetProperty("temperature")?.GetValue(weatherObj) ?? 0),
            FeelsLike = Convert.ToDouble(weatherObj.GetType().GetProperty("feelsLike")?.GetValue(weatherObj) ?? 0),
            Description = weatherObj.GetType().GetProperty("description")?.GetValue(weatherObj)?.ToString() ?? "",
            Icon = weatherObj.GetType().GetProperty("icon")?.GetValue(weatherObj)?.ToString() ?? "01d",
            Humidity = Convert.ToInt32(weatherObj.GetType().GetProperty("humidity")?.GetValue(weatherObj) ?? 0),
            WindSpeed = Convert.ToDouble(weatherObj.GetType().GetProperty("windSpeed")?.GetValue(weatherObj) ?? 0),
            Pressure = Convert.ToInt32(weatherObj.GetType().GetProperty("pressure")?.GetValue(weatherObj) ?? 0),
            Clouds = Convert.ToInt32(weatherObj.GetType().GetProperty("clouds")?.GetValue(weatherObj) ?? 0),
            Visibility = Convert.ToInt32(weatherObj.GetType().GetProperty("visibility")?.GetValue(weatherObj) ?? 10000),
            Timestamp = Convert.ToDateTime(weatherObj.GetType().GetProperty("timestamp")?.GetValue(weatherObj) ?? DateTime.UtcNow),
            Latitude = lat,
            Longitude = lon,
            UserId = userId
        };

        return Ok(new ApiResponse<SharedWeatherData>
        {
            Success = true,
            Data = weatherData,
            Message = "Weather fetched successfully"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching weather by coordinates");
        return StatusCode(500, new ApiResponse<SharedWeatherData>
        {
            Success = false,
            Message = "Failed to fetch weather"
        });
    }
}

        // ✅ GET /api/weather/forecast/{city}
        [HttpGet("forecast/{city}")]
        public async Task<IActionResult> GetForecast(string city)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new ApiResponse<ForecastData>
                    {
                        Success = false,
                        Message = "City parameter is required"
                    });
                }

                _logger.LogInformation("Fetching forecast for: {City}", city);

                var forecastObj = await _weatherService.GetForecastAsync(city);
                
                if (forecastObj == null)
                {
                    return NotFound(new ApiResponse<ForecastData>
                    {
                        Success = false,
                        Message = $"Forecast not found for city: {city}"
                    });
                }

                // Convert dynamic object to ForecastData
                var forecastData = new ForecastData
                {
                    City = forecastObj.GetType().GetProperty("city")?.GetValue(forecastObj)?.ToString() ?? city,
                    Country = forecastObj.GetType().GetProperty("country")?.GetValue(forecastObj)?.ToString() ?? "",
                    Forecasts = new List<ForecastItem>(),
                    Timestamp = DateTime.UtcNow
                };

                // Extract forecast items
                var forecastsProperty = forecastObj.GetType().GetProperty("forecasts")?.GetValue(forecastObj);
                if (forecastsProperty is IEnumerable<object> forecasts)
                {
                    foreach (var item in forecasts)
                    {
                        var forecastItem = new ForecastItem
                        {
                            DateTime = Convert.ToDateTime(item.GetType().GetProperty("dateTime")?.GetValue(item) ?? DateTime.UtcNow),
                            Temperature = Convert.ToDouble(item.GetType().GetProperty("temperature")?.GetValue(item) ?? 0),
                            FeelsLike = Convert.ToDouble(item.GetType().GetProperty("feelsLike")?.GetValue(item) ?? 0),
                            TempMin = Convert.ToDouble(item.GetType().GetProperty("tempMin")?.GetValue(item) ?? 0),
                            TempMax = Convert.ToDouble(item.GetType().GetProperty("tempMax")?.GetValue(item) ?? 0),
                            Description = item.GetType().GetProperty("description")?.GetValue(item)?.ToString() ?? "",
                            Humidity = Convert.ToInt32(item.GetType().GetProperty("humidity")?.GetValue(item) ?? 0),
                            WindSpeed = Convert.ToDouble(item.GetType().GetProperty("windSpeed")?.GetValue(item) ?? 0),
                            Clouds = Convert.ToInt32(item.GetType().GetProperty("clouds")?.GetValue(item) ?? 0)
                        };
                        forecastData.Forecasts.Add(forecastItem);
                    }
                }

                return Ok(new ApiResponse<ForecastData>
                {
                    Success = true,
                    Data = forecastData,
                    Message = "Forecast fetched successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching forecast for {City}", city);
                return StatusCode(500, new ApiResponse<ForecastData>
                {
                    Success = false,
                    Message = "Failed to fetch forecast"
                });
            }
        }

        // ✅ GET /api/weather/top-cities - THIS WAS MISSING!
        [HttpGet("top-cities")]
        public async Task<IActionResult> GetTopCities()
        {
            try
            {
                _logger.LogInformation("Fetching top cities");

                // Popular cities list
                var cityNames = new[]
                {
                    "London", "New York", "Tokyo", "Paris", "Sydney", 
                    "Dubai", "Singapore", "Mumbai", "Los Angeles", "Toronto"
                };

                var topCities = new List<TopCity>();

                // Fetch current weather for each city
                foreach (var cityName in cityNames)
                {
                    try
                    {
                        var weatherObj = await _weatherService.GetCurrentWeatherAsync(cityName);
                        if (weatherObj != null)
                        {
                            var topCity = new TopCity
                            {
                                Name = weatherObj.GetType().GetProperty("city")?.GetValue(weatherObj)?.ToString() ?? cityName,
                                Country = weatherObj.GetType().GetProperty("country")?.GetValue(weatherObj)?.ToString() ?? "",
                                Temperature = Convert.ToDouble(weatherObj.GetType().GetProperty("temperature")?.GetValue(weatherObj) ?? 0),
                                Description = weatherObj.GetType().GetProperty("description")?.GetValue(weatherObj)?.ToString() ?? "",
                                Icon = "" // OpenWeatherMap icon can be added here if needed
                            };
                            topCities.Add(topCity);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch weather for {City}", cityName);
                        // Add city with default values if API call fails
                        topCities.Add(new TopCity
                        {
                            Name = cityName,
                            Country = "",
                            Temperature = 0,
                            Description = "Data unavailable"
                        });
                    }
                }

                return Ok(new ApiResponse<List<TopCity>>
                {
                    Success = true,
                    Data = topCities,
                    Message = "Top cities fetched successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top cities");
                return StatusCode(500, new ApiResponse<List<TopCity>>
                {
                    Success = false,
                    Message = "Failed to fetch top cities"
                });
            }
        }

        // ✅ GET /api/weather/recent?limit=50
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentWeather([FromQuery] int limit = 50)
        {
            try
            {
                _logger.LogInformation("Fetching recent weather, limit: {Limit}", limit);

                var collection = _database.GetCollection<SharedWeatherData>("weatherData");
                var recentWeather = await collection
                    .Find(_ => true)
                    .SortByDescending(w => w.Timestamp)
                    .Limit(limit)
                    .ToListAsync();

                return Ok(new ApiResponse<List<SharedWeatherData>>
                {
                    Success = true,
                    Data = recentWeather,
                    Message = $"Fetched {recentWeather.Count} recent weather records"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent weather");
                return StatusCode(500, new ApiResponse<List<SharedWeatherData>>
                {
                    Success = false,
                    Message = "Failed to fetch recent weather"
                });
            }
        }

        // ✅ Test endpoint
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { 
                message = "Weather Controller working!", 
                timestamp = DateTime.UtcNow,
                endpoints = new[]
                {
                    "GET /api/weather/current/{city}",
                    "GET /api/weather/coordinates?lat=...&lon=...",
                    "GET /api/weather/forecast/{city}",
                    "GET /api/weather/top-cities",
                    "GET /api/weather/recent?limit=50",
                    "GET /api/weather/test"
                }
            });
        }
    }
}