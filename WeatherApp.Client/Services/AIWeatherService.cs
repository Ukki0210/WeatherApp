using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WeatherApp.Client.Services
{
    public class AIWeatherService
    {
        private readonly HttpClient _httpClient;

        public AIWeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetWeatherInsightsAsync(WeatherInsightRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/AIWeather/insights", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AIInsightResponse>();
                    return result?.Insights ?? "Unable to generate insights.";
                }
                else
                {
                    return "Failed to generate insights. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "Error connecting to AI service. Please try again later.";
            }
        }
    }

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
        [JsonPropertyName("insights")]
        public string Insights { get; set; } = string.Empty;
        
        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; }
    }
}
