using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace AIChatbot.Utility
{
    public sealed class JwtTokenService
    {
        private readonly SigningCredentials _credentials;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtTokenService(SymmetricSecurityKey signingKey, string issuer, string audience)
        {
            _credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            _issuer = issuer;
            _audience = audience;
        }

        public (string Value, DateTime ExpiresAt) CreateToken(AuthUser user)
        {
            var issuedAt = DateTime.UtcNow;
            var expiresAt = issuedAt.AddHours(1);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Email)
            };
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: issuedAt,
                expires: expiresAt,
                signingCredentials: _credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
