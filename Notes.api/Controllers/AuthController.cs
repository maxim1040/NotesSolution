using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notes.Api.Data;
using Notes.Api.DTOs;
using Notes.Api.Models;
using Notes.Api.Services;


namespace Notes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userMgr;
    private readonly SignInManager<ApplicationUser> _signInMgr;
    private readonly IJwtTokenService _jwt;
    private readonly AppDbContext _db;


    public AuthController(UserManager<ApplicationUser> u, SignInManager<ApplicationUser> s, IJwtTokenService j, AppDbContext db)
    { _userMgr = u; _signInMgr = s; _jwt = j; _db = db; }


    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var user = new ApplicationUser { UserName = req.Email, Email = req.Email };
        var result = await _userMgr.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return BadRequest(new { errors });
        }
        return Ok();
    }


    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _userMgr.Users.FirstOrDefaultAsync(x => x.Email == req.Email);
        if (user == null) return Unauthorized();


        var ok = await _signInMgr.CheckPasswordSignInAsync(user, req.Password, false);
        if (!ok.Succeeded) return Unauthorized();


        var (access, exp) = _jwt.CreateAccessToken(user);
        var refresh = _jwt.CreateRefreshToken(user.Id);
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync();


        return new AuthResponse(access, exp, refresh.Token);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest req)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == req.RefreshToken);
        if (token == null || !token.IsActive) return Unauthorized();


        var user = await _userMgr.FindByIdAsync(token.UserId);
        if (user == null) return Unauthorized();


        token.RevokedUtc = DateTime.UtcNow;
        var newRefresh = _jwt.CreateRefreshToken(user.Id);
        _db.RefreshTokens.Add(newRefresh);


        var (access, exp) = _jwt.CreateAccessToken(user);
        await _db.SaveChangesAsync();


        return new AuthResponse(access, exp, newRefresh.Token);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        var user = userId == null ? null : await _userMgr.FindByIdAsync(userId);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.Email });
    }
}