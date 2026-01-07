using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIWeatherController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIWeatherController> _logger;

        public AIWeatherController(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AIWeatherController> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("insights")]
        public async Task<ActionResult<AIInsightResponse>> GetWeatherInsights([FromBody] WeatherInsightRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating AI insights for {request.CityName}");

                var prompt = BuildWeatherPrompt(request);
                var aiResponse = await CallGeminiAPI(prompt);

                return Ok(new AIInsightResponse
                {
                    Insights = aiResponse,
                    GeneratedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating AI insights: {ex.Message}");
                return StatusCode(500, new AIInsightResponse
                {
                    Insights = "Unable to generate insights at this time. Please try again later.",
                    GeneratedAt = DateTime.UtcNow
                });
            }
        }

        private string BuildWeatherPrompt(WeatherInsightRequest request)
        {
            return $@"You are a professional meteorologist and weather advisor. Analyze the following weather data and provide practical insights in a friendly, conversational tone.

Weather Data for {request.CityName}:
- Temperature: {request.Temperature:F1}°C (Feels like: {request.FeelsLike:F1}°C)
- Condition: {request.Condition}
- Humidity: {request.Humidity}%
- Wind Speed: {request.WindSpeed:F1} m/s
- Visibility: {request.Visibility / 1000} km

Provide a response with exactly 4 sections (keep each section to 2-3 sentences max):

1. **Weather Overview**: Brief description of current conditions
2. **Activity Recommendations**: What outdoor/indoor activities are suitable
3. **Clothing Advice**: What to wear based on temperature and conditions
4. **Health & Safety Tips**: Any weather-related health considerations

Keep the response under 200 words total. Be specific, practical, and friendly. Use emojis sparingly (max 3 total).";
        }

        private async Task<string> CallGeminiAPI(string prompt)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Gemini API key not configured, using fallback response");
                return GenerateFallbackInsights(prompt);
            }

            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 500,
                        topP = 0.8,
                        topK = 10
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";
                
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Gemini API Response: {jsonResponse}");
                    
                    var result = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    var insightText = result?.Candidates?[0]?.Content?.Parts?[0]?.Text;
                    
                    if (!string.IsNullOrEmpty(insightText))
                    {
                        return insightText;
                    }
                    else
                    {
                        _logger.LogWarning("Gemini API returned empty response");
                        return GenerateFallbackInsights(prompt);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Gemini API returned status code: {response.StatusCode}, Error: {errorContent}");
                    return GenerateFallbackInsights(prompt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling Gemini API: {ex.Message}");
                return GenerateFallbackInsights(prompt);
            }
        }

        private string GenerateFallbackInsights(string prompt)
        {
            // Extract weather data from prompt
            var lines = prompt.Split('\n');
            var tempLine = lines.FirstOrDefault(l => l.Contains("Temperature:"));
            var conditionLine = lines.FirstOrDefault(l => l.Contains("Condition:"));
            var humidityLine = lines.FirstOrDefault(l => l.Contains("Humidity:"));
            var windLine = lines.FirstOrDefault(l => l.Contains("Wind Speed:"));
            var cityLine = lines.FirstOrDefault(l => l.Contains("Weather Data for"));

            var city = "your location";
            var temp = 20.0;
            var condition = "Clear";
            var humidity = 50;
            var windSpeed = 5.0;

            if (cityLine != null)
            {
                var cityMatch = System.Text.RegularExpressions.Regex.Match(cityLine, @"Weather Data for (.+):");
                if (cityMatch.Success)
                    city = cityMatch.Groups[1].Value.Trim();
            }

            if (tempLine != null)
            {
                var tempMatch = System.Text.RegularExpressions.Regex.Match(tempLine, @"(-?\d+\.?\d*)°C");
                if (tempMatch.Success)
                    double.TryParse(tempMatch.Groups[1].Value, out temp);
            }

            if (conditionLine != null)
            {
                var parts = conditionLine.Split(':');
                if (parts.Length > 1)
                    condition = parts[1].Trim();
            }

            if (humidityLine != null)
            {
                var humidMatch = System.Text.RegularExpressions.Regex.Match(humidityLine, @"(\d+)%");
                if (humidMatch.Success)
                    int.TryParse(humidMatch.Groups[1].Value, out humidity);
            }

            if (windLine != null)
            {
                var windMatch = System.Text.RegularExpressions.Regex.Match(windLine, @"(\d+\.?\d*) m/s");
                if (windMatch.Success)
                    double.TryParse(windMatch.Groups[1].Value, out windSpeed);
            }

            return GenerateRuleBasedInsights(city, temp, condition, humidity, windSpeed);
        }

        private string GenerateRuleBasedInsights(string city, double temp, string condition, int humidity, double windSpeed)
        {
            var insights = new StringBuilder();

            // Weather Overview
            insights.AppendLine("**Weather Overview**");
            if (temp < 10)
                insights.AppendLine($"It's quite cold in {city} at {temp:F0}°C with {condition.ToLower()} conditions. Bundle up if you're heading outside! ❄️");
            else if (temp < 20)
                insights.AppendLine($"Pleasant weather in {city} at {temp:F0}°C with {condition.ToLower()} skies. Perfect for outdoor activities with light layers.");
            else if (temp < 30)
                insights.AppendLine($"Warm and comfortable at {temp:F0}°C in {city}. The {condition.ToLower()} conditions make it ideal for being outdoors. ☀️");
            else
                insights.AppendLine($"Hot weather in {city} at {temp:F0}°C. Stay hydrated and seek shade during peak hours. 🌡️");

            insights.AppendLine();

            // Activity Recommendations
            insights.AppendLine("**Activity Recommendations**");
            if (condition.ToLower().Contains("rain") || condition.ToLower().Contains("drizzle"))
                insights.AppendLine("Indoor activities recommended. Great day for museums, cafes, or catching up on indoor hobbies. Bring an umbrella if you must go out!");
            else if (condition.ToLower().Contains("clear") || condition.ToLower().Contains("sun"))
                insights.AppendLine("Perfect weather for outdoor activities! Consider going for a walk, cycling, or having a picnic in the park.");
            else if (condition.ToLower().Contains("cloud"))
                insights.AppendLine("Good conditions for outdoor exercise. The clouds provide natural shade, making it comfortable for jogging or sports.");
            else if (condition.ToLower().Contains("snow"))
                insights.AppendLine("Winter activities are possible! Be cautious on slippery surfaces. Great for winter sports if conditions allow.");
            else
                insights.AppendLine("Moderate conditions for both indoor and outdoor activities. Plan according to your preferences.");

            insights.AppendLine();

            // Clothing Advice
            insights.AppendLine("**Clothing Advice**");
            if (temp < 5)
                insights.AppendLine("Heavy winter coat, scarf, gloves, and warm layers essential. Wear insulated boots to keep your feet warm.");
            else if (temp < 15)
                insights.AppendLine("Wear a jacket or sweater with long pants. Layer up so you can adjust as needed throughout the day.");
            else if (temp < 25)
                insights.AppendLine("Light jacket or long sleeves recommended. T-shirt and jeans work well for this comfortable temperature.");
            else if (temp < 32)
                insights.AppendLine("Light, breathable clothing is perfect. T-shirt and shorts will keep you cool. Consider a hat for sun protection.");
            else
                insights.AppendLine("Wear minimal, light-colored, loose-fitting clothes. Light colors reflect heat better. Sunscreen and a hat are essential!");

            insights.AppendLine();

            // Health & Safety
            insights.AppendLine("**Health & Safety Tips**");
            if (temp > 32)
                insights.AppendLine("Heat advisory: Stay hydrated with at least 2-3 liters of water daily. Avoid strenuous outdoor activity between 11 AM - 4 PM. Watch for heat exhaustion symptoms.");
            else if (temp < 5)
                insights.AppendLine("Cold weather warning: Limit time outdoors to prevent frostbite. Keep extremities covered and warm up gradually when indoors.");
            else if (humidity > 80 && temp > 25)
                insights.AppendLine("High humidity makes it feel warmer than the actual temperature. Stay hydrated and take frequent breaks in air-conditioned spaces.");
            else if (windSpeed > 15)
                insights.AppendLine("Strong winds present. Secure loose objects and be cautious when walking. Consider postponing outdoor activities if wind gusts are severe.");
            else if (condition.ToLower().Contains("rain"))
                insights.AppendLine("Wet conditions increase slip risks. Walk carefully on smooth surfaces. Ensure good visibility if driving. Stay indoors during thunderstorms.");
            else
                insights.AppendLine("Conditions are generally safe for outdoor activities. Remember to stay hydrated and take regular breaks if exercising. Apply sunscreen if sunny.");

            return insights.ToString();
        }
    }

    // DTOs
    public class WeatherInsightRequest
    {
        public string CityName { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public string Condition { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public int Visibility { get; set; }
    }

    public class AIInsightResponse
    {
        public string Insights { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    // Gemini Response Models
    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
