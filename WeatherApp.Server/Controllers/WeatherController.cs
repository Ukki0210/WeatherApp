using Microsoft.AspNetCore.Mvc;
using WeatherApp.Server.Services;

namespace WeatherApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]  // This makes it /api/weather
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(WeatherService weatherService, ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentWeather([FromQuery] string city)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { error = "City parameter is required" });
                }

                _logger.LogInformation("Fetching weather for: {City}", city);

                var weather = await _weatherService.GetCurrentWeatherAsync(city);
                
                if (weather == null)
                {
                    return NotFound(new { error = $"Weather not found for city: {city}" });
                }

                return Ok(weather);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather for {City}", city);
                return StatusCode(500, new { error = "Failed to fetch weather", details = ex.Message });
            }
        }

        [HttpGet("forecast")]
        public async Task<IActionResult> GetForecast([FromQuery] string city)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { error = "City parameter is required" });
                }

                var forecast = await _weatherService.GetForecastAsync(city);
                
                if (forecast == null)
                {
                    return NotFound(new { error = $"Forecast not found for city: {city}" });
                }

                return Ok(forecast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching forecast for {City}", city);
                return StatusCode(500, new { error = "Failed to fetch forecast", details = ex.Message });
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { 
                message = "Weather Controller working!", 
                timestamp = DateTime.UtcNow 
            });
        }
    }
}
