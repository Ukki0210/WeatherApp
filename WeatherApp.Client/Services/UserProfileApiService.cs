using System.Net.Http.Json;
using WeatherApp.Shared.Models;

namespace WeatherApp.Client.Services
{
    public class UserProfileApiService
    {
        private readonly HttpClient _httpClient;

        public UserProfileApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UserProfile?> GetProfileAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserProfile>>(
                    $"api/userprofile/{userId}");
                return response?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching profile: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateOrUpdateProfileAsync(UserProfile profile)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/userprofile", profile);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfile>>();
                return result?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddFavoriteCityAsync(string userId, string city)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"api/userprofile/{userId}/favorites",
                    new { City = city });
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding favorite: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveFavoriteCityAsync(string userId, string city)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(
                    $"api/userprofile/{userId}/favorites/{city}");
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing favorite: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdatePreferencesAsync(
            string userId,
            bool darkMode,
            string temperatureUnit,
            string? defaultCity = null)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"api/userprofile/{userId}/preferences",
                    new
                    {
                        DarkMode = darkMode,
                        TemperatureUnit = temperatureUnit,
                        DefaultCity = defaultCity
                    });
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating preferences: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddAlertAsync(string userId, WeatherAlert alert)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"api/userprofile/{userId}/alerts",
                    alert);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding alert: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveAlertAsync(string userId, string city, string alertType)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(
                    $"api/userprofile/{userId}/alerts?city={city}&alertType={alertType}");
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing alert: {ex.Message}");
                return false;
            }
        }

        public async Task<List<AlertHistory>> GetAlertHistoryAsync(string userId, int limit = 50)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<AlertHistory>>>(
                    $"api/userprofile/{userId}/alert-history?limit={limit}");
                return response?.Data ?? new List<AlertHistory>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching alert history: {ex.Message}");
                return new List<AlertHistory>();
            }
        }

        public async Task<bool> MarkAlertAsReadAsync(string alertId)
        {
            try
            {
                var response = await _httpClient.PutAsync(
                    $"api/userprofile/alert/{alertId}/read",
                    null);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking alert as read: {ex.Message}");
                return false;
            }
        }
    }
}