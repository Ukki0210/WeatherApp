using Microsoft.AspNetCore.Mvc;
using WeatherApp.Server.Services;
using WeatherApp.Shared.Models;

namespace WeatherApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SupabaseAuthService _authService;
        private readonly UserProfileService _userProfileService;

        public AuthController(
            SupabaseAuthService authService,
            UserProfileService userProfileService)
        {
            _authService = authService;
            _userProfileService = userProfileService;
        }

        
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || 
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Email and password are required"
                });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Password must be at least 6 characters"
                });
            }

            var result = await _authService.RegisterAsync(request);

            if (result.Success)
            {
                // Create user profile in MongoDB
                var profile = new UserProfile
                {
                    SupabaseUserId = result.UserId,
                    Email = request.Email,
                    DisplayName = request.DisplayName,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow
                };

                await _userProfileService.CreateOrUpdateProfileAsync(profile);
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || 
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Email and password are required"
                });
            }

            var result = await _authService.LoginAsync(request);

            if (result.Success)
            {
                // Update last login
                await _userProfileService.UpdateLastLoginAsync(result.UserId);
            }

            return result.Success ? Ok(result) : Unauthorized(result);
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var result = await _authService.LogoutAsync();
            return result ? Ok(new { message = "Logged out successfully" }) 
                         : BadRequest(new { message = "Logout failed" });
        }

        // GET: api/auth/validate
        [HttpGet("validate")]
        public async Task<ActionResult> ValidateToken()
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid or expired token" });
            }

            return Ok(new { userId = user.Id, email = user.Email });
        }
    }
}