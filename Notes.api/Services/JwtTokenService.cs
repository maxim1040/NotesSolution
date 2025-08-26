using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Notes.Api.Models;


namespace Notes.Api.Services;
public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; }
    public int RefreshExpiresDays { get; set; }
}


public interface IJwtTokenService
{
    (string token, DateTime expiresUtc) CreateAccessToken(ApplicationUser user);
    RefreshToken CreateRefreshToken(string userId);
}


public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opt;
    public JwtTokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;


    public (string token, DateTime expiresUtc) CreateAccessToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_opt.ExpiresMinutes);


        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new(ClaimTypes.Email, user.Email ?? "")
        };


        var jwt = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expires);
    }

    public RefreshToken CreateRefreshToken(string userId) => new()
    {
        Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
        UserId = userId,
        ExpiresUtc = DateTime.UtcNow.AddDays(_opt.RefreshExpiresDays)
    };
}