using System.Security.Claims;
using AIChatbot.DTO;
using AIChatbot.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace AIChatbot.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthRepository _users;
        private readonly PasswordHasher<AuthUser> _passwordHasher;
        private readonly JwtTokenService _tokenService;

        public AuthController(
            AuthRepository users,
            PasswordHasher<AuthUser> passwordHasher,
            JwtTokenService tokenService)
        {
            _users = users;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest request)
        {
            var user = new AuthUser { Email = request.Email.Trim().ToLowerInvariant() };
            var passwordHash = _passwordHasher.HashPassword(user, request.Password);

            try
            {
                await _users.CreateAsync(user.Email, passwordHash);
            }
            catch (PostgresException exception) when (exception.SqlState == "23505")
            {
                return Conflict(new { error = "An account with this email already exists." });
            }

            var createdUser = await _users.FindByEmailAsync(user.Email)
                ?? throw new InvalidOperationException("The newly created user could not be loaded.");
            return Ok(CreateAuthResponse(createdUser));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            var user = await _users.FindByEmailAsync(request.Email);
            if (user is null ||
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { error = "The email or password is incorrect." });
            }

            return Ok(CreateAuthResponse(user));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
            {
                return Unauthorized();
            }

            var user = await _users.FindByIdAsync(id);
            return user is null
                ? Unauthorized()
                : Ok(new { id = user.Id, email = user.Email });
        }

        private object CreateAuthResponse(AuthUser user)
        {
            var token = _tokenService.CreateToken(user);
            return new
            {
                id = user.Id,
                email = user.Email,
                token = token.Value,
                expiresAt = token.ExpiresAt
            };
        }
    }
}
