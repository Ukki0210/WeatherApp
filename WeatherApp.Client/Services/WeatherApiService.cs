using System.Net.Http.Json;
using WeatherApp.Shared.Models;

namespace WeatherApp.Client.Services
{
    public class WeatherApiService
    {
        private readonly HttpClient _httpClient;

        public WeatherApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherData?> GetCurrentWeatherAsync(string city, string? userId = null)
        {
            try
            {
                var url = $"api/weather/current/{city}";
                if (!string.IsNullOrEmpty(userId))
                {
                    url += $"?userId={userId}";
                }

                // 🔍 LOG EXACT URL
                Console.WriteLine("📡 WEATHER API CALL:");
                Console.WriteLine("BaseAddress: " + _httpClient.BaseAddress);
                Console.WriteLine("Endpoint: " + url);
                Console.WriteLine("Full URL: " + new Uri(_httpClient.BaseAddress!, url));

                var httpResponse = await _httpClient.GetAsync(url);

                // 🔍 LOG STATUS CODE
                Console.WriteLine("HTTP Status: " + httpResponse.StatusCode);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    Console.WriteLine("❌ Response Body:");
                    Console.WriteLine(errorBody);
                    return null;
                }

                var response =
                    await httpResponse.Content.ReadFromJsonAsync<ApiResponse<WeatherData>>();

                return response?.Data;
            }
            catch (Exception ex)
            {
                // 🔥 FULL ERROR (not swallowed)
                Console.WriteLine("🔥 WEATHER API EXCEPTION:");
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public async Task<WeatherData?> GetWeatherByCoordinatesAsync(
            double lat, double lon, string? userId = null)
        {
            try
            {
                var url = $"api/weather/coordinates?lat={lat}&lon={lon}";
                if (!string.IsNullOrEmpty(userId))
                {
                    url += $"&userId={userId}";
                }

                Console.WriteLine("📡 WEATHER COORD API CALL:");
                Console.WriteLine(new Uri(_httpClient.BaseAddress!, url));

                var httpResponse = await _httpClient.GetAsync(url);
                Console.WriteLine("HTTP Status: " + httpResponse.StatusCode);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine(await httpResponse.Content.ReadAsStringAsync());
                    return null;
                }

                var response =
                    await httpResponse.Content.ReadFromJsonAsync<ApiResponse<WeatherData>>();

                return response?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 COORD API EXCEPTION:");
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public async Task<ForecastData?> GetForecastAsync(string city)
        {
            try
            {
                var url = $"api/weather/forecast/{city}";
                Console.WriteLine("📡 FORECAST API CALL:");
                Console.WriteLine(new Uri(_httpClient.BaseAddress!, url));

                var response =
                    await _httpClient.GetFromJsonAsync<ApiResponse<ForecastData>>(url);

                return response?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 FORECAST API EXCEPTION:");
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public async Task<List<TopCity>> GetTopCitiesAsync()
        {
            try
            {
                var url = "api/weather/top-cities";
                Console.WriteLine("📡 TOP CITIES API CALL:");
                Console.WriteLine(new Uri(_httpClient.BaseAddress!, url));

                var response =
                    await _httpClient.GetFromJsonAsync<ApiResponse<List<TopCity>>>(url);

                return response?.Data ?? new List<TopCity>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 TOP CITIES API EXCEPTION:");
                Console.WriteLine(ex.ToString());
                return new List<TopCity>();
            }
        }

        public async Task<List<WeatherData>> GetRecentWeatherAsync(int limit = 50)
        {
            try
            {
                var url = $"api/weather/recent?limit={limit}";
                Console.WriteLine("📡 RECENT WEATHER API CALL:");
                Console.WriteLine(new Uri(_httpClient.BaseAddress!, url));

                var response =
                    await _httpClient.GetFromJsonAsync<ApiResponse<List<WeatherData>>>(url);

                return response?.Data ?? new List<WeatherData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 RECENT WEATHER API EXCEPTION:");
                Console.WriteLine(ex.ToString());
                return new List<WeatherData>();
            }
        }
    }
}
