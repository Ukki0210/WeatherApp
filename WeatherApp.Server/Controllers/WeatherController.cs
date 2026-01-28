using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WeatherApp.Server.Services;
using WeatherApp.Shared.Models;

namespace WeatherApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;
        private readonly IAlertService _alertService;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(
            WeatherService weatherService, 
            IAlertService alertService,
            ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _alertService = alertService;
            _logger = logger;
        }

        // GET: api/weather/current/{city}
        [HttpGet("current/{city}")]
        public async Task<ActionResult<ApiResponse<WeatherData>>> GetCurrentWeather(
            string city, [FromQuery] string? userId = null)
        {
            var weather = await _weatherService.FetchCurrentWeatherAsync(city, userId);

            if (weather == null)
            {
                return NotFound(new ApiResponse<WeatherData>
                {
                    Success = false,
                    Message = $"Could not fetch weather data for {city}"
                });
            }

            // ✅ CHECK FOR ALERTS AUTOMATICALLY
            try
            {
                await _alertService.CheckAndTriggerAlertsAsync(
                    city,
                    weather.Temperature,
                    weather.WindSpeed,
                    weather.Humidity
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking alerts for {City}", city);
                // Don't fail the request if alert checking fails
            }

            return Ok(new ApiResponse<WeatherData>
            {
                Success = true,
                Data = weather,
                Message = "Weather data retrieved successfully"
            });
        }

        // GET: api/weather/coordinates
        [HttpGet("coordinates")]
        public async Task<ActionResult<ApiResponse<WeatherData>>> GetWeatherByCoordinates(
            [FromQuery] double lat,
            [FromQuery] double lon,
            [FromQuery] string? userId = null)
        {
            var weather = await _weatherService.FetchWeatherByCoordinatesAsync(lat, lon, userId);

            if (weather == null)
            {
                return NotFound(new ApiResponse<WeatherData>
                {
                    Success = false,
                    Message = "Could not fetch weather data for the given coordinates"
                });
            }

            // ✅ CHECK FOR ALERTS AUTOMATICALLY
            try
            {
                await _alertService.CheckAndTriggerAlertsAsync(
                    weather.City,
                    weather.Temperature,
                    weather.WindSpeed,
                    weather.Humidity
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking alerts for coordinates {Lat},{Lon}", lat, lon);
            }

            return Ok(new ApiResponse<WeatherData>
            {
                Success = true,
                Data = weather,
                Message = "Weather data retrieved successfully"
            });
        }

        // GET: api/weather/forecast/{city}
        [HttpGet("forecast/{city}")]
        public async Task<ActionResult<ApiResponse<ForecastData>>> GetForecast(string city)
        {
            var forecast = await _weatherService.FetchForecastAsync(city);

            if (forecast == null)
            {
                return NotFound(new ApiResponse<ForecastData>
                {
                    Success = false,
                    Message = $"Could not fetch forecast data for {city}"
                });
            }

            return Ok(new ApiResponse<ForecastData>
            {
                Success = true,
                Data = forecast,
                Message = "Forecast data retrieved successfully"
            });
        }

        // GET: api/weather/top-cities
        [HttpGet("top-cities")]
        public async Task<ActionResult<ApiResponse<List<TopCity>>>> GetTopCities()
        {
            var cities = await _weatherService.GetTopCitiesWeatherAsync();

            return Ok(new ApiResponse<List<TopCity>>
            {
                Success = true,
                Data = cities,
                Message = "Top cities data retrieved successfully"
            });
        }

        // GET: api/weather/recent
        [HttpGet("recent")]
        public async Task<ActionResult<ApiResponse<List<WeatherData>>>> GetRecentWeather(
            [FromQuery] int limit = 50)
        {
            var weather = await _weatherService.GetRecentWeatherAsync(limit);

            return Ok(new ApiResponse<List<WeatherData>>
            {
                Success = true,
                Data = weather,
                Message = "Recent weather data retrieved successfully"
            });
        }

        // ✅ TEST EMAIL ENDPOINT
        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail([FromServices] IEmailService emailService)
        {
            try
            {
                await emailService.SendWeatherAlertAsync(
                    "ukki0210@gmail.com",
                    "Utkarsh",
                    "Delhi",
                    "🔥 Extreme heat warning: 45°C. This is a test alert from Weather App! Stay hydrated and avoid outdoor activities during peak hours."
                );
                
                return Ok(new { 
                    success = true, 
                    message = "Test email sent successfully! Check your inbox at ukki0210@gmail.com" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email");
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        // ✅ MANUAL TRIGGER FOR FAVORITE CITY ALERTS
        [HttpGet("check-favorites-now")]
        public async Task<IActionResult> CheckFavoritesNow(
            [FromServices] UserProfileService userProfileService,
            [FromServices] IEmailService emailService,
            [FromQuery] string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { message = "userId is required" });

                var user = await userProfileService.GetProfileAsync(userId);
                
                if (user == null)
                    return NotFound(new { message = "User not found" });

                if (user.FavoriteCities == null || !user.FavoriteCities.Any())
                    return Ok(new { message = "No favorite cities found" });

                var alertsSent = new List<string>();

                foreach (var city in user.FavoriteCities)
                {
                    var weather = await _weatherService.FetchCurrentWeatherAsync(city, userId);
                    
                    if (weather == null) continue;

                    bool isSevere = false;
                    string alertMessage = "";

                    // Check for severe conditions
                    if (weather.Temperature > 40)
                    {
                        isSevere = true;
                        alertMessage = $"🔥 Extreme heat warning: {weather.Temperature}°C. Stay hydrated and avoid outdoor activities.";
                    }
                    else if (weather.Temperature < 5)
                    {
                        isSevere = true;
                        alertMessage = $"❄️ Cold weather advisory: {weather.Temperature}°C. Dress warmly and stay indoors if possible.";
                    }
                    else if (weather.WindSpeed > 50)
                    {
                        isSevere = true;
                        alertMessage = $"💨 High wind warning: {weather.WindSpeed} km/h. Secure loose objects and avoid driving.";
                    }
                    else if (weather.Humidity > 90)
                    {
                        isSevere = true;
                        alertMessage = $"🌧️ Heavy rain expected: {weather.Humidity}% humidity. Carry an umbrella and avoid flood-prone areas.";
                    }

                    if (isSevere)
                    {
                        await emailService.SendWeatherAlertAsync(
                            user.Email,
                            user.DisplayName,
                            city,
                            alertMessage
                        );

                        alertsSent.Add($"{city}: {alertMessage}");
                        _logger.LogInformation("Alert sent to {Email} for {City}", user.Email, city);
                    }
                }

                if (alertsSent.Any())
                {
                    return Ok(new { 
                        success = true, 
                        message = $"Sent {alertsSent.Count} alert(s) to {user.Email}",
                        alerts = alertsSent
                    });
                }
                else
                {
                    return Ok(new { 
                        success = true, 
                        message = "No severe weather detected in your favorite cities" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking favorites");
                return BadRequest(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }
    }
}
