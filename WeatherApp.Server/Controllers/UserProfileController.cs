using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WeatherApp.Server.Services;
using WeatherApp.Shared.Models;

namespace WeatherApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfileController : ControllerBase
    {
        private readonly UserProfileService _userProfileService;

        public UserProfileController(UserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        // GET: api/userprofile/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<ApiResponse<UserProfile>>> GetProfile(string userId)
        {
            var profile = await _userProfileService.GetProfileAsync(userId);

            if (profile == null)
            {
                return NotFound(new ApiResponse<UserProfile>
                {
                    Success = false,
                    Message = "Profile not found"
                });
            }

            return Ok(new ApiResponse<UserProfile>
            {
                Success = true,
                Data = profile,
                Message = "Profile retrieved successfully"
            });
        }

        // POST: api/userprofile
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserProfile>>> CreateOrUpdateProfile(
            [FromBody] UserProfile profile)
        {
            var result = await _userProfileService.CreateOrUpdateProfileAsync(profile);

            return Ok(new ApiResponse<UserProfile>
            {
                Success = true,
                Data = result,
                Message = "Profile saved successfully"
            });
        }

        // POST: api/userprofile/{userId}/favorites
        [HttpPost("{userId}/favorites")]
        public async Task<ActionResult<ApiResponse<bool>>> AddFavoriteCity(
            string userId,
            [FromBody] FavoriteCityRequest request)
        {
            var result = await _userProfileService.AddFavoriteCityAsync(userId, request.City);

            if (!result)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Could not add favorite city"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "City added to favorites"
            });
        }

        // DELETE: api/userprofile/{userId}/favorites/{city}
        [HttpDelete("{userId}/favorites/{city}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveFavoriteCity(
            string userId,
            string city)
        {
            var result = await _userProfileService.RemoveFavoriteCityAsync(userId, city);

            if (!result)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Could not remove favorite city"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "City removed from favorites"
            });
        }

        // PUT: api/userprofile/{userId}/preferences
        [HttpPut("{userId}/preferences")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdatePreferences(
            string userId,
            [FromBody] PreferencesRequest request)
        {
            var result = await _userProfileService.UpdatePreferencesAsync(
                userId,
                request.DarkMode,
                request.TemperatureUnit,
                request.DefaultCity);

            if (!result)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Could not update preferences"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Preferences updated successfully"
            });
        }

        // POST: api/userprofile/{userId}/alerts
        [HttpPost("{userId}/alerts")]
        public async Task<ActionResult<ApiResponse<bool>>> AddAlert(
            string userId,
            [FromBody] WeatherAlert alert)
        {
            var result = await _userProfileService.AddAlertAsync(userId, alert);

            if (!result)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Could not add alert"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Alert added successfully"
            });
        }

        // DELETE: api/userprofile/{userId}/alerts
        [HttpDelete("{userId}/alerts")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveAlert(
            string userId,
            [FromQuery] string city,
            [FromQuery] string alertType)
        {
            var result = await _userProfileService.RemoveAlertAsync(userId, city, alertType);

            if (!result)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Could not remove alert"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Alert removed successfully"
            });
        }

        // GET: api/userprofile/{userId}/alert-history
        [HttpGet("{userId}/alert-history")]
        public async Task<ActionResult<ApiResponse<List<AlertHistory>>>> GetAlertHistory(
            string userId,
            [FromQuery] int limit = 50)
        {
            var history = await _userProfileService.GetAlertHistoryAsync(userId, limit);

            return Ok(new ApiResponse<List<AlertHistory>>
            {
                Success = true,
                Data = history,
                Message = "Alert history retrieved successfully"
            });
        }

        // PUT: api/userprofile/alert/{alertId}/read
        [HttpPut("alert/{alertId}/read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAlertAsRead(string alertId)
        {
            var result = await _userProfileService.MarkAlertAsReadAsync(alertId);

            if (!result)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Could not mark alert as read"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Alert marked as read"
            });
        }
    }

    // Request models for this controller
    public class FavoriteCityRequest
    {
        public string City { get; set; } = string.Empty;
    }

    public class PreferencesRequest
    {
        public bool DarkMode { get; set; }
        public string TemperatureUnit { get; set; } = "celsius";
        public string? DefaultCity { get; set; }
    }
}