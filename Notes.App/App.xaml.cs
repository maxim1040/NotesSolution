using System.IdentityModel.Tokens.Jwt;
using Notes.App.Views;

namespace Notes.App;
public partial class App : Application
{
    public App(IServiceProvider sp)
    {
        InitializeComponent();

        var token = SecureStorage.GetAsync(Constants.AccessTokenKey).GetAwaiter().GetResult();
        if (IsTokenValid(token))
        {
            // (optioneel) koppel LocalDb aan user_id uit SecureStorage
            var uid = SecureStorage.GetAsync("user_id").GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(uid))
                sp.GetRequiredService<Notes.App.Services.LocalDb>().UseUserAsync(uid).GetAwaiter().GetResult();

            MainPage = new AppShell();
        }
        else
        {
            MainPage = sp.GetRequiredService<LoginPage>();
        }
    }

    private static bool IsTokenValid(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt)) return false;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var t = handler.ReadJwtToken(jwt);
            var exp = t.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (exp is null) return false;
            var expires = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp));
            return expires > DateTimeOffset.UtcNow.AddMinutes(1);
        }
        catch { return false; }
    }
}
