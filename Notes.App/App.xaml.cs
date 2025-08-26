using Notes.App.Views;
using System.IdentityModel.Tokens.Jwt;

namespace Notes.App;

public partial class App : Application
{
    private readonly IServiceProvider _sp;

    public App(IServiceProvider sp)
    {
        InitializeComponent();
        _sp = sp;

        // tijdelijke “splash/loading” pagina zodat constructor licht blijft
        MainPage = new ContentPage
        {
            Content = new ActivityIndicator
            {
                IsRunning = true,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            }
        };

        // Start async init op de UI-thread
        MainThread.BeginInvokeOnMainThread(async () => await InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync(Constants.AccessTokenKey);
            if (IsTokenValid(token))
            {
                // per-user LocalDb koppelen
                var uid = await SecureStorage.GetAsync("user_id");
                if (string.IsNullOrWhiteSpace(uid))
                {
                    var auth = _sp.GetRequiredService<Services.AuthService>();
                    uid = await auth.EnsureUserIdAsync(); // stuurt Bearer mee (zie eerdere patch)
                }

                if (!string.IsNullOrWhiteSpace(uid))
                {
                    var db = _sp.GetRequiredService<Services.LocalDb>();
                    await db.UseUserAsync(uid); // per-user DB kiezen
                }

                MainPage = new AppShell();
            }
            else
            {
                MainPage = _sp.GetRequiredService<LoginPage>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Startup error: " + ex);
            // Vang alles af en val terug op login i.p.v. crash
            MainPage = _sp.GetRequiredService<LoginPage>();
        }
    }

    private static bool IsTokenValid(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt)) return false;
        try
        {
            var t = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            var expClaim = t.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (expClaim is null) return false;
            var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim));
            return exp > DateTimeOffset.UtcNow.AddMinutes(1);
        }
        catch { return false; }
    }
}
