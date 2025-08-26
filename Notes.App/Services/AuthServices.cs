// Notes.App/Services/AuthService.cs
using System.Net.Http.Json;
using System.Text.Json;

namespace Notes.App.Services;

public class AuthService
{
    private readonly HttpClient _http;
    public AuthService(HttpClient http) => _http = http;

    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);

    public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresUtc);
    public record MeResponse(string userId, string email);

    // ---- REGISTER ----
    // Retourneert (ok, errors). errors bevat Identity-teksten als het faalt.
    public async Task<(bool ok, List<string> errors)> RegisterAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync($"{Constants.ApiBase}/api/auth/register",
            new RegisterRequest(email, password));

        if (res.IsSuccessStatusCode)
            return (true, new List<string>());

        // Probeer { errors: [...] } te lezen
        try
        {
            var doc = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (doc != null && doc.TryGetValue("errors", out var errsObj) &&
                errsObj is JsonElement el && el.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in el.EnumerateArray())
                    list.Add(item.GetString() ?? string.Empty);
                return (false, list.Where(s => !string.IsNullOrWhiteSpace(s)).ToList());
            }
        }
        catch { /* val door naar fallback */ }

        var text = await res.Content.ReadAsStringAsync();
        return (false, new List<string> { string.IsNullOrWhiteSpace(text) ? "Registratie mislukt." : text });
    }

    // ---- LOGIN ----
    public async Task<bool> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync($"{Constants.ApiBase}/api/auth/login",
            new LoginRequest(email, password));
        if (!res.IsSuccessStatusCode) return false;

        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (data is null || string.IsNullOrWhiteSpace(data.AccessToken)) return false;

        await SecureStorage.SetAsync(Constants.AccessTokenKey, data.AccessToken);
        await SecureStorage.SetAsync(Constants.RefreshTokenKey, data.RefreshToken);
        return true;
    }

    // ---- TOKENS ----
    public async Task<string?> GetAccessTokenAsync()
        => await SecureStorage.GetAsync(Constants.AccessTokenKey);

    public async Task<bool> RefreshAsync()
    {
        var refresh = await SecureStorage.GetAsync(Constants.RefreshTokenKey);
        if (string.IsNullOrWhiteSpace(refresh)) return false;

        var res = await _http.PostAsJsonAsync($"{Constants.ApiBase}/api/auth/refresh",
            new RefreshRequest(refresh));
        if (!res.IsSuccessStatusCode) return false;

        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (data is null || string.IsNullOrWhiteSpace(data.AccessToken)) return false;

        await SecureStorage.SetAsync(Constants.AccessTokenKey, data.AccessToken);
        await SecureStorage.SetAsync(Constants.RefreshTokenKey, data.RefreshToken);
        return true;
    }

    // ---- USER INFO ----
    public async Task<string?> EnsureUserIdAsync()
    {
        // cache?
        var cached = await SecureStorage.GetAsync("user_id");
        if (!string.IsNullOrWhiteSpace(cached)) return cached;

        var token = await SecureStorage.GetAsync(Constants.AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token)) return null;

        // Stuur expliciet de Authorization header mee
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{Constants.ApiBase}/api/auth/me");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return null;

        var me = await res.Content.ReadFromJsonAsync<MeResponse>();
        if (me is null || string.IsNullOrWhiteSpace(me.userId)) return null;

        await SecureStorage.SetAsync("user_id", me.userId);
        return me.userId;
    }

    // ---- LOGOUT ----
    public Task LogoutAsync()
    {
        SecureStorage.Remove(Constants.AccessTokenKey);
        SecureStorage.Remove(Constants.RefreshTokenKey);
        SecureStorage.Remove("user_id");
        return Task.CompletedTask;
    }
}
