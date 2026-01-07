using MongoDB.Driver;
using WeatherApp.Shared.Models;
using Newtonsoft.Json.Linq;

namespace WeatherApp.Server.Services
{
    public class WeatherService
    {
        private readonly IMongoCollection<WeatherData> _weatherCollection;
        private readonly IMongoCollection<ForecastData> _forecastCollection;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public WeatherService(
            IMongoDatabase database,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _weatherCollection = database.GetCollection<WeatherData>(
                configuration["MongoDatabase:WeatherCollection"] ?? "weatherData");
            _forecastCollection = database.GetCollection<ForecastData>(
                configuration["MongoDatabase:ForecastCollection"] ?? "forecasts");
            _httpClient = httpClient;
            _configuration = configuration;
            _apiKey = configuration["OpenWeatherMap:ApiKey"] ?? "";
            _baseUrl = configuration["OpenWeatherMap:BaseUrl"] ?? "";
        }

        // Fetch current weather by city
        public async Task<WeatherData?> FetchCurrentWeatherAsync(string city, string? userId = null)
        {
            try
            {
                var url = $"{_baseUrl}/weather?q={city}&appid={_apiKey}&units=metric";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var weatherData = new WeatherData
                {
                    City = json["name"]?.ToString() ?? city,
                    Country = json["sys"]?["country"]?.ToString() ?? "",
                    Latitude = json["coord"]?["lat"]?.ToObject<double>() ?? 0,
                    Longitude = json["coord"]?["lon"]?.ToObject<double>() ?? 0,
                    Temperature = json["main"]?["temp"]?.ToObject<double>() ?? 0,
                    FeelsLike = json["main"]?["feels_like"]?.ToObject<double>() ?? 0,
                    TempMin = json["main"]?["temp_min"]?.ToObject<double>() ?? 0,
                    TempMax = json["main"]?["temp_max"]?.ToObject<double>() ?? 0,
                    Description = json["weather"]?[0]?["description"]?.ToString() ?? "",
                    Humidity = json["main"]?["humidity"]?.ToObject<int>() ?? 0,
                    WindSpeed = json["wind"]?["speed"]?.ToObject<double>() ?? 0,
                    Pressure = json["main"]?["pressure"]?.ToObject<int>() ?? 0,
                    Visibility = json["visibility"]?.ToObject<int>() ?? 0,
                    Clouds = json["clouds"]?["all"]?.ToObject<int>() ?? 0,
                    Icon = json["weather"]?[0]?["icon"]?.ToString() ?? "",
                    Sunrise = DateTimeOffset.FromUnixTimeSeconds(
                        json["sys"]?["sunrise"]?.ToObject<long>() ?? 0).DateTime,
                    Sunset = DateTimeOffset.FromUnixTimeSeconds(
                        json["sys"]?["sunset"]?.ToObject<long>() ?? 0).DateTime,
                    Timestamp = DateTime.UtcNow,
                    UserId = userId
                };

                await _weatherCollection.InsertOneAsync(weatherData);
                return weatherData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching weather: {ex.Message}");
                return null;
            }
        }

        // Fetch weather by coordinates
        public async Task<WeatherData?> FetchWeatherByCoordinatesAsync(
            double lat, double lon, string? userId = null)
        {
            try
            {
                var url = $"{_baseUrl}/weather?lat={lat}&lon={lon}&appid={_apiKey}&units=metric";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var weatherData = new WeatherData
                {
                    City = json["name"]?.ToString() ?? "Unknown",
                    Country = json["sys"]?["country"]?.ToString() ?? "",
                    Latitude = lat,
                    Longitude = lon,
                    Temperature = json["main"]?["temp"]?.ToObject<double>() ?? 0,
                    FeelsLike = json["main"]?["feels_like"]?.ToObject<double>() ?? 0,
                    TempMin = json["main"]?["temp_min"]?.ToObject<double>() ?? 0,
                    TempMax = json["main"]?["temp_max"]?.ToObject<double>() ?? 0,
                    Description = json["weather"]?[0]?["description"]?.ToString() ?? "",
                    Humidity = json["main"]?["humidity"]?.ToObject<int>() ?? 0,
                    WindSpeed = json["wind"]?["speed"]?.ToObject<double>() ?? 0,
                    Pressure = json["main"]?["pressure"]?.ToObject<int>() ?? 0,
                    Visibility = json["visibility"]?.ToObject<int>() ?? 0,
                    Clouds = json["clouds"]?["all"]?.ToObject<int>() ?? 0,
                    Icon = json["weather"]?[0]?["icon"]?.ToString() ?? "",
                    Sunrise = DateTimeOffset.FromUnixTimeSeconds(
                        json["sys"]?["sunrise"]?.ToObject<long>() ?? 0).DateTime,
                    Sunset = DateTimeOffset.FromUnixTimeSeconds(
                        json["sys"]?["sunset"]?.ToObject<long>() ?? 0).DateTime,
                    Timestamp = DateTime.UtcNow,
                    UserId = userId
                };

                await _weatherCollection.InsertOneAsync(weatherData);
                return weatherData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching weather: {ex.Message}");
                return null;
            }
        }

        // Fetch 5-day forecast
        public async Task<ForecastData?> FetchForecastAsync(string city)
        {
            try
            {
                var url = $"{_baseUrl}/forecast?q={city}&appid={_apiKey}&units=metric";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var forecastData = new ForecastData
                {
                    City = json["city"]?["name"]?.ToString() ?? city,
                    Country = json["city"]?["country"]?.ToString() ?? "",
                    Timestamp = DateTime.UtcNow
                };

                var list = json["list"] as JArray;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        var forecast = new ForecastItem
                        {
                            DateTime = DateTimeOffset.FromUnixTimeSeconds(
                                item["dt"]?.ToObject<long>() ?? 0).DateTime,
                            Temperature = item["main"]?["temp"]?.ToObject<double>() ?? 0,
                            FeelsLike = item["main"]?["feels_like"]?.ToObject<double>() ?? 0,
                            TempMin = item["main"]?["temp_min"]?.ToObject<double>() ?? 0,
                            TempMax = item["main"]?["temp_max"]?.ToObject<double>() ?? 0,
                            Description = item["weather"]?[0]?["description"]?.ToString() ?? "",
                            Humidity = item["main"]?["humidity"]?.ToObject<int>() ?? 0,
                            WindSpeed = item["wind"]?["speed"]?.ToObject<double>() ?? 0,
                            Clouds = item["clouds"]?["all"]?.ToObject<int>() ?? 0,
                            Icon = item["weather"]?[0]?["icon"]?.ToString() ?? "",
                            RainVolume = item["rain"]?["3h"]?.ToObject<double>() ?? 0
                        };
                        forecastData.Forecasts.Add(forecast);
                    }
                }

                await _forecastCollection.InsertOneAsync(forecastData);
                return forecastData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching forecast: {ex.Message}");
                return null;
            }
        }

        // Get top 5 cities weather
        public async Task<List<TopCity>> GetTopCitiesWeatherAsync()
        {
            var topCities = new List<TopCity>();
            var cities = _configuration.GetSection("TopCities").Get<List<string>>() ?? new List<string>();

            foreach (var cityCountry in cities)
            {
                try
                {
                    var url = $"{_baseUrl}/weather?q={cityCountry}&appid={_apiKey}&units=metric";
                    var response = await _httpClient.GetStringAsync(url);
                    var json = JObject.Parse(response);

                    topCities.Add(new TopCity
                    {
                        Name = json["name"]?.ToString() ?? "",
                        Country = json["sys"]?["country"]?.ToString() ?? "",
                        Temperature = json["main"]?["temp"]?.ToObject<double>() ?? 0,
                        Description = json["weather"]?[0]?["description"]?.ToString() ?? "",
                        Icon = json["weather"]?[0]?["icon"]?.ToString() ?? ""
                    });
                }
                catch
                {
                    continue;
                }
            }

            return topCities;
        }

        // Get recent weather data
        public async Task<List<WeatherData>> GetRecentWeatherAsync(int limit = 50)
        {
            return await _weatherCollection.Find(_ => true)
                .SortByDescending(w => w.Timestamp)
                .Limit(limit)
                .ToListAsync();
        }
    }
}