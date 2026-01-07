using Supabase;
using Supabase.Gotrue;
using WeatherApp.Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using GotrueClient = Supabase.Gotrue.Client;

namespace WeatherApp.Server.Services
{
    public class SupabaseAuthService
    {
        private readonly Supabase.Client _supabase;
        private readonly IConfiguration _configuration;

        public SupabaseAuthService(Supabase.Client supabase, IConfiguration configuration)
        {
            _supabase = supabase;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var options = new SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        { "display_name", request.DisplayName }
                    }
                };

                var session = await _supabase.Auth.SignUp(request.Email, request.Password, options);

                if (session?.User == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Registration failed. Please try again."
                    };
                }

                var token = GenerateJwtToken(session.User.Id, request.Email);

                return new AuthResponse
                {
                    Success = true,
                    Token = token,
                    UserId = session.User.Id,
                    Email = request.Email,
                    Message = "Registration successful!"
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Registration error: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var session = await _supabase.Auth.SignIn(request.Email, request.Password);

                if (session?.User == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };
                }

                var token = GenerateJwtToken(session.User.Id, request.Email);

                return new AuthResponse
                {
                    Success = true,
                    Token = token,
                    UserId = session.User.Id,
                    Email = request.Email,
                    Message = "Login successful!"
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Login error: {ex.Message}"
                };
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                await _supabase.Auth.SignOut();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Supabase.Gotrue.User?> GetCurrentUserAsync()
        {
            try
            {
                var user = _supabase.Auth.CurrentUser;
                return user;
            }
            catch
            {
                return null;
            }
        }

        private string GenerateJwtToken(string userId, string email)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"] ?? ""));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}