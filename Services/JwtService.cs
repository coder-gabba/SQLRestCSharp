using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SqlAPI.Services
{
    /// <summary>
    /// Service for generating and validating JWT tokens
    /// </summary>
    public class JwtService
    {
        /// <summary>
        /// Generates a JWT token for the specified username with Admin role
        /// </summary>
        /// <param name="username">The username to include in the token</param>
        /// <returns>A JWT token string</returns>
        public string GenerateToken(string username)
        {
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
                ?? throw new InvalidOperationException("JWT Key is not configured");
            var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                ?? throw new InvalidOperationException("JWT Issuer is not configured");
            var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                ?? throw new InvalidOperationException("JWT Audience is not configured");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(30),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
