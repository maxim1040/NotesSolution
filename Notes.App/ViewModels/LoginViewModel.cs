using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Notes.App.Services;

namespace Notes.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _auth;
    private readonly IServiceProvider _sp;

    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool isBusy;

    public IAsyncRelayCommand LoginAsyncCommand { get; }
    public IAsyncRelayCommand RegisterAsyncCommand { get; }

    public LoginViewModel(AuthService auth)
    {
        _auth = auth;
        LoginAsyncCommand = new AsyncRelayCommand(LoginAsync, CanExec);
        RegisterAsyncCommand = new AsyncRelayCommand(RegisterAsync, CanExec);
    }

    private bool CanExec() => !IsBusy;

    private async Task LoginAsync()
    {
        if (IsBusy) return; IsBusy = true;
        try
        {
            var ok = await _auth.LoginAsync(Email, Password);
            if (!ok)
            {
                await Application.Current.MainPage.DisplayAlert("Login", "Mislukt", "OK");
                return;
            }

            var uid = await _auth.EnsureUserIdAsync();
            if (!string.IsNullOrWhiteSpace(uid))
            {
                var db = _sp.GetRequiredService<LocalDb>();
                await db.UseUserAsync(uid);
            }

            Application.Current.MainPage = new AppShell();
            await Shell.Current.GoToAsync("//notes");
        }
        finally { IsBusy = false; }
    }

    private async Task RegisterAsync()
    {
        if (IsBusy) return; IsBusy = true; RegisterAsyncCommand.NotifyCanExecuteChanged();
        try
        {
            var (ok, errors) = await _auth.RegisterAsync(Email, Password);
            if (ok)
            {
                if (await _auth.LoginAsync(Email, Password))
                {
                    Application.Current!.MainPage = new AppShell();
                    await Shell.Current.GoToAsync("//notes");
                }
                return;
            }
            var msg = errors.Count == 0 ? "Registratie mislukt." : string.Join("\n• ", errors.Prepend("Problemen:"));
            await Application.Current!.MainPage.DisplayAlert("Registratie", msg, "OK");
        }
        finally { IsBusy = false; RegisterAsyncCommand.NotifyCanExecuteChanged(); }
    }
}