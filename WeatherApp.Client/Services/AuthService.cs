using System.Net.Http.Json;
using Blazored.LocalStorage;
using WeatherApp.Shared.Models;

namespace WeatherApp.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private string? _currentUserId;
        private string? _currentUserEmail;

        public event Action? OnAuthStateChanged;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_currentUserId);
        public string? CurrentUserId => _currentUserId;
        public string? CurrentUserEmail => _currentUserEmail;

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (result?.Success == true)
                {
                    await SaveAuthDataAsync(result);
                }

                return result ?? new AuthResponse 
                { 
                    Success = false, 
                    Message = "Registration failed" 
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (result?.Success == true)
                {
                    await SaveAuthDataAsync(result);
                }

                return result ?? new AuthResponse 
                { 
                    Success = false, 
                    Message = "Login failed" 
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userId");
            await _localStorage.RemoveItemAsync("userEmail");
            
            _currentUserId = null;
            _currentUserEmail = null;
            
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            OnAuthStateChanged?.Invoke();
        }

        public async Task InitializeAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            var userId = await _localStorage.GetItemAsync<string>("userId");
            var userEmail = await _localStorage.GetItemAsync<string>("userEmail");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userId))
            {
                _currentUserId = userId;
                _currentUserEmail = userEmail;
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                
                OnAuthStateChanged?.Invoke();
            }
        }

        private async Task SaveAuthDataAsync(AuthResponse authResponse)
        {
            await _localStorage.SetItemAsync("authToken", authResponse.Token);
            await _localStorage.SetItemAsync("userId", authResponse.UserId);
            await _localStorage.SetItemAsync("userEmail", authResponse.Email);

            _currentUserId = authResponse.UserId;
            _currentUserEmail = authResponse.Email;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);

            OnAuthStateChanged?.Invoke();
        }
    }
}