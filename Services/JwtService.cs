using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SqlAPI.Models;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    private SecurityTokenDescriptor TokenDescriptor(IEnumerable<Claim> claims)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(30),
        Issuer = _config["Jwt:Issuer"],
        Audience = _config["Jwt:Audience"],
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")
                    )), SecurityAlgorithms.HmacSha256Signature)
    };
        return tokenDescriptor;
    }
    public string GenerateToken(Person user)
    {
        var securityKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims:
            [
                
            ],
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public string GenerateToken(string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        Claim[] c = [
                new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Admin")
            ];
        var token = tokenHandler.CreateToken(TokenDescriptor(c));
        return tokenHandler.WriteToken(token);
    }

}
