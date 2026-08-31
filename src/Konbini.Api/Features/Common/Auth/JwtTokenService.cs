using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Konbini.Api.Features.Auth.Models;
using Microsoft.IdentityModel.Tokens;

namespace Konbini.Api.Features.Common.Auth;

public interface IJwtTokenService
{
    string CreateToken(User user);
}

public sealed class JwtTokenService(IConfiguration config) : IJwtTokenService
{
    public string CreateToken(User user)
    {
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret 未設定");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
