using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notes.Api.Data;
using Notes.Api.Models;
using Notes.Api.Services;
using System.Security.Claims;

namespace Notes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IJwtTokenService _jwt;
    private readonly AppDbContext _db;

    public AuthController(UserManager<ApplicationUser> users, IJwtTokenService jwt, AppDbContext db)
    {
        _users = users;
        _jwt = jwt;
        _db = db;
    }


    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);
    public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresUtc);

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = new ApplicationUser { UserName = req.Email, Email = req.Email };
        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { errors });
        }
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user is null) return Unauthorized();

        var ok = await _users.CheckPasswordAsync(user, req.Password);
        if (!ok) return Unauthorized();

        var (access, expires) = _jwt.CreateAccessToken(user);
        var refresh = _jwt.CreateRefreshToken(user.Id);


        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync();

        return new AuthResponse(access, refresh.Token, expires);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest();

        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == req.RefreshToken);
        if (rt is null || rt.ExpiresUtc <= DateTime.UtcNow) return Unauthorized();

        var user = await _users.FindByIdAsync(rt.UserId);
        if (user is null) return Unauthorized();

        var (access, expires) = _jwt.CreateAccessToken(user);
        var newRt = _jwt.CreateRefreshToken(user.Id);

        _db.RefreshTokens.Remove(rt);
        _db.RefreshTokens.Add(newRt);
        await _db.SaveChangesAsync();

        return new AuthResponse(access, newRt.Token, expires);
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        return Ok(new { userId = uid, email });
    }
}
