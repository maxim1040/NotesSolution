namespace Notes.Api.DTOs;
public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, DateTime ExpiresUtc, string RefreshToken);
public record RefreshRequest(string RefreshToken);