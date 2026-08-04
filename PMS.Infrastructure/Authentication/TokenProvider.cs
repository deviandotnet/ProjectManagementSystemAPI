using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PMS.Application.Abstractions.Authentication;
using PMS.Domain.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace PMS.Infrastructure.Authentication
{
    internal sealed class TokenProvider(IConfiguration configuration) : ITokenProvider
    {
        public string CreateAccessToken(User user)
        {
            string rawSecret = configuration["Jwt:Secret"]!;
            string secretKey = string.IsNullOrWhiteSpace(rawSecret)
                ? "super_secret_default_key_at_least_32_bytes_long!"
                : rawSecret;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            string rawIssuer = configuration["Jwt:Issuer"] ?? string.Empty;
            string issuer = string.IsNullOrWhiteSpace(rawIssuer) ? "PMS" : rawIssuer;

            string rawAudience = configuration["Jwt:Audience"] ?? string.Empty;
            string audience = string.IsNullOrWhiteSpace(rawAudience) ? "PMS" : rawAudience;

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            int expirationMinutes = configuration.GetValue<int>("Jwt:ExpirationInMinutes");
            if (expirationMinutes <= 0)
            {
                expirationMinutes = 60;
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(
                [
                    new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new System.Security.Claims.Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new System.Security.Claims.Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
                    new System.Security.Claims.Claim(JwtRegisteredClaimNames.FamilyName, user.LastName)
                ]),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                SigningCredentials = credentials,
                Issuer = issuer,
                Audience = audience
            };

            var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();

            string accessToken = handler.CreateToken(tokenDescriptor);

            return accessToken;

        }

        public string CreateRefreshToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(randomBytes);
        }
    }
}
