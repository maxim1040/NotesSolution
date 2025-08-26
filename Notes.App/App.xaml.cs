using Notes.App.Views;

namespace Notes.App;
public partial class App : Application
{
    public App(IServiceProvider sp)
    {
        InitializeComponent();

        var token = SecureStorage.GetAsync(Constants.AccessTokenKey).GetAwaiter().GetResult();
        MainPage = string.IsNullOrEmpty(token)
            ? sp.GetRequiredService<LoginPage>()
            : new AppShell();
    }
}