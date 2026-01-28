using MongoDB.Driver;
using WeatherApp.Shared.Models;

namespace WeatherApp.Server.Services
{
    public class UserProfileService
    {
        private readonly IMongoCollection<UserProfile> _userCollection;
        private readonly IMongoCollection<AlertHistory> _alertHistoryCollection;

        public UserProfileService(IMongoDatabase database, IConfiguration configuration)
        {
            _userCollection = database.GetCollection<UserProfile>(
                configuration["MongoDatabase:UserCollection"] ?? "users");
            _alertHistoryCollection = database.GetCollection<AlertHistory>(
                configuration["MongoDatabase:AlertHistoryCollection"] ?? "alertHistory");
        }

        // Create or update user profile
        public async Task<UserProfile> CreateOrUpdateProfileAsync(UserProfile profile)
        {
            var existingProfile = await _userCollection
                .Find(u => u.SupabaseUserId == profile.SupabaseUserId)
                .FirstOrDefaultAsync();

            if (existingProfile != null)
            {
                profile.Id = existingProfile.Id;
                profile.CreatedAt = existingProfile.CreatedAt;
                await _userCollection.ReplaceOneAsync(
                    u => u.Id == existingProfile.Id, profile);
                return profile;
            }

            await _userCollection.InsertOneAsync(profile);
            return profile;
        }

        // Get user profile
        public async Task<UserProfile?> GetProfileAsync(string supabaseUserId)
        {
            return await _userCollection
                .Find(u => u.SupabaseUserId == supabaseUserId)
                .FirstOrDefaultAsync();
        }

        // Add favorite city
        public async Task<bool> AddFavoriteCityAsync(string supabaseUserId, string city)
        {
            var profile = await GetProfileAsync(supabaseUserId);
            if (profile == null) return false;

            if (!profile.FavoriteCities.Contains(city))
            {
                profile.FavoriteCities.Add(city);
                var result = await _userCollection.ReplaceOneAsync(
                    u => u.SupabaseUserId == supabaseUserId, profile);
                return result.ModifiedCount > 0;
            }

            return false;
        }

        // Remove favorite city
        public async Task<bool> RemoveFavoriteCityAsync(string supabaseUserId, string city)
        {
            var profile = await GetProfileAsync(supabaseUserId);
            if (profile == null) return false;

            if (profile.FavoriteCities.Remove(city))
            {
                var result = await _userCollection.ReplaceOneAsync(
                    u => u.SupabaseUserId == supabaseUserId, profile);
                return result.ModifiedCount > 0;
            }

            return false;
        }

        // Update user preferences
        public async Task<bool> UpdatePreferencesAsync(
            string supabaseUserId,
            bool darkMode,
            string temperatureUnit,
            string? defaultCity = null)
        {
            var profile = await GetProfileAsync(supabaseUserId);
            if (profile == null) return false;

            profile.DarkMode = darkMode;
            profile.TemperatureUnit = temperatureUnit;
            if (defaultCity != null)
            {
                profile.DefaultCity = defaultCity;
            }

            var result = await _userCollection.ReplaceOneAsync(
                u => u.SupabaseUserId == supabaseUserId, profile);
            return result.ModifiedCount > 0;
        }

        // Add weather alert
        public async Task<bool> AddAlertAsync(string supabaseUserId, WeatherAlert alert)
        {
            var profile = await GetProfileAsync(supabaseUserId);
            if (profile == null) return false;

            profile.Alerts.Add(alert);
            var result = await _userCollection.ReplaceOneAsync(
                u => u.SupabaseUserId == supabaseUserId, profile);
            return result.ModifiedCount > 0;
        }

        // Remove weather alert
        public async Task<bool> RemoveAlertAsync(string supabaseUserId, string city, string alertType)
        {
            var profile = await GetProfileAsync(supabaseUserId);
            if (profile == null) return false;

            var alert = profile.Alerts.FirstOrDefault(a => a.City == city && a.AlertType == alertType);
            if (alert != null && profile.Alerts.Remove(alert))
            {
                var result = await _userCollection.ReplaceOneAsync(
                    u => u.SupabaseUserId == supabaseUserId, profile);
                return result.ModifiedCount > 0;
            }

            return false;
        }

        // Get alert history
        public async Task<List<AlertHistory>> GetAlertHistoryAsync(
            string userId, int limit = 50)
        {
            return await _alertHistoryCollection
                .Find(a => a.UserId == userId)
                .SortByDescending(a => a.TriggeredAt)
                .Limit(limit)
                .ToListAsync();
        }

        // Mark alert as read
        public async Task<bool> MarkAlertAsReadAsync(string alertId)
        {
            var result = await _alertHistoryCollection.UpdateOneAsync(
                a => a.Id == alertId,
                Builders<AlertHistory>.Update.Set(a => a.IsRead, true));
            return result.ModifiedCount > 0;
        }

        // Update last login
        public async Task UpdateLastLoginAsync(string supabaseUserId)
        {
            await _userCollection.UpdateOneAsync(
                u => u.SupabaseUserId == supabaseUserId,
                Builders<UserProfile>.Update.Set(u => u.LastLogin, DateTime.UtcNow));
        }

        // Get all users - FIXED
        public async Task<List<UserProfile>> GetAllUsersAsync()
        {
            return await _userCollection.Find(_ => true).ToListAsync();
        }
    }
}
