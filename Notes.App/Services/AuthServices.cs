using System.Net.Http.Json;
using System.Text.Json;


namespace Notes.App.Services;
public class AuthService
{
    private readonly HttpClient _http;
    public AuthService(HttpClient http) { _http = http; }
    public record MeResponse(string userId, string email);


    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string AccessToken, DateTime ExpiresUtc, string RefreshToken);
    public record RefreshRequest(string RefreshToken);
    public record RegisterRequest(string Email, string Password);


    public async Task<bool> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync($"{Constants.ApiBase}/api/auth/login", new LoginRequest(email, password));
        if (!res.IsSuccessStatusCode) return false;
        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (data == null) return false;
        await SecureStorage.SetAsync(Constants.AccessTokenKey, data.AccessToken);
        await SecureStorage.SetAsync(Constants.RefreshTokenKey, data.RefreshToken);
        return true;
    }


    public async Task<bool> RefreshAsync()
    {
        var refresh = await SecureStorage.GetAsync(Constants.RefreshTokenKey);
        if (string.IsNullOrWhiteSpace(refresh)) return false;
        var res = await _http.PostAsJsonAsync($"{Constants.ApiBase}/api/auth/refresh", new RefreshRequest(refresh));
        if (!res.IsSuccessStatusCode) return false;
        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (data == null) return false;
        await SecureStorage.SetAsync(Constants.AccessTokenKey, data.AccessToken);
        await SecureStorage.SetAsync(Constants.RefreshTokenKey, data.RefreshToken);
        return true;
    }

    public async Task<string?> EnsureUserIdAsync()
    {
        var cached = await SecureStorage.GetAsync("user_id");
        if (!string.IsNullOrWhiteSpace(cached)) return cached;

        var me = await _http.GetFromJsonAsync<MeResponse>($"{Constants.ApiBase}/api/auth/me");
        if (me is null || string.IsNullOrWhiteSpace(me.userId)) return null;

        await SecureStorage.SetAsync("user_id", me.userId);
        return me.userId;
    }

    public Task LogoutAsync()
    {
        SecureStorage.Remove(Constants.AccessTokenKey);
        SecureStorage.Remove(Constants.RefreshTokenKey);
        SecureStorage.Remove("user_id");
        return Task.CompletedTask;
    }


    public async Task<(bool ok, List<string> errors)> RegisterAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync($"{Constants.ApiBase}/api/auth/register",
            new RegisterRequest(email, password));

        if (res.IsSuccessStatusCode)
            return (true, new List<string>());

        // probeer { errors: [...] }
        try
        {
            var json = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (json != null && json.TryGetValue("errors", out var errsObj) && errsObj is JsonElement el && el.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in el.EnumerateArray())
                    list.Add(item.GetString() ?? "");
                return (false, list.Where(s => !string.IsNullOrWhiteSpace(s)).ToList());
            }
        }
        catch { /* valt door naar fallback */ }

        // Fallback: ProblemDetails of plain text
        var text = await res.Content.ReadAsStringAsync();
        return (false, new List<string> { string.IsNullOrWhiteSpace(text) ? "Registratie mislukt." : text });
    }


    public Task<string?> GetAccessTokenAsync() => SecureStorage.GetAsync(Constants.AccessTokenKey);
}