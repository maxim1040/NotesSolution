using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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

    // Injecteer HIER ook IServiceProvider
    public LoginViewModel(AuthService auth, IServiceProvider sp)
    {
        _auth = auth;
        _sp = sp;

        LoginAsyncCommand = new AsyncRelayCommand(LoginAsync, CanExec);
        RegisterAsyncCommand = new AsyncRelayCommand(RegisterAsync, CanExec);
    }

    private bool CanExec() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        LoginAsyncCommand.NotifyCanExecuteChanged();
        RegisterAsyncCommand.NotifyCanExecuteChanged();
    }

    private async Task LoginAsync()
    {
        if (IsBusy) return; IsBusy = true;
        try
        {
            var ok = await _auth.LoginAsync(Email.Trim(), Password);
            if (!ok)
            {
                await Application.Current!.MainPage.DisplayAlert("Login", "Mislukt", "OK");
                return;
            }

            // Haal userId op (met Bearer) en koppel LocalDb aan deze user
            var uid = await _auth.EnsureUserIdAsync();
            if (!string.IsNullOrWhiteSpace(uid))
            {
                var db = _sp.GetRequiredService<LocalDb>();
                await db.UseUserAsync(uid);
            }

            Application.Current!.MainPage = new AppShell();
            await Shell.Current.GoToAsync("//notes");
        }
        catch (Exception ex)
        {
            // Toon volledige fout (handig bij 401/500 of netwerkproblemen)
            await Application.Current!.MainPage.DisplayAlert("Fout bij inloggen", ex.ToString(), "OK");
        }
        finally { IsBusy = false; }
    }

    private async Task RegisterAsync()
    {
        if (IsBusy) return; IsBusy = true;
        try
        {
            var (ok, errors) = await _auth.RegisterAsync(Email.Trim(), Password);
            if (!ok)
            {
                var msg = errors.Count == 0 ? "Registratie mislukt." : string.Join("\n• ", errors.Prepend("Problemen:"));
                await Application.Current!.MainPage.DisplayAlert("Registratie", msg, "OK");
                return;
            }

            // Na registratie meteen inloggen en DB koppelen
            var logged = await _auth.LoginAsync(Email.Trim(), Password);
            if (logged)
            {
                var uid = await _auth.EnsureUserIdAsync();
                if (!string.IsNullOrWhiteSpace(uid))
                {
                    var db = _sp.GetRequiredService<LocalDb>();
                    await db.UseUserAsync(uid);
                }

                Application.Current!.MainPage = new AppShell();
                await Shell.Current.GoToAsync("//notes");
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage.DisplayAlert("Fout bij registreren", ex.ToString(), "OK");
        }
        finally { IsBusy = false; }
    }
}
