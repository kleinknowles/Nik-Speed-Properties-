using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NikSpeed.Api.Data.Entities;

namespace NikSpeed.Api.Services;

public class TokenService(IConfiguration configuration)
{
    public string Create(User user)
    {
        var jwt = configuration.GetSection("Jwt");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"],
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Name, user.Name)],
            expires: DateTime.UtcNow.AddDays(7), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
