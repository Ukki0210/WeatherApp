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

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
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
    }
}