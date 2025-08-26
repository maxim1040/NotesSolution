using Microsoft.Extensions.Logging;
using Notes.App.Services;
using Notes.App.ViewModels;
using Notes.App.Views;

namespace Notes.App;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));
#if DEBUG
        var insecure = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        builder.Services.AddHttpClient<AuthService>()
            .ConfigurePrimaryHttpMessageHandler(() => insecure);
        builder.Services.AddHttpClient<ApiClient>()
            .ConfigurePrimaryHttpMessageHandler(() => insecure)
            .AddHttpMessageHandler<TokenHandler>();
#else
builder.Services.AddHttpClient<AuthService>();
builder.Services.AddHttpClient<ApiClient>()
    .AddHttpMessageHandler<TokenHandler>();
#endif

        builder.Services.AddSingleton<SyncService>();

        // ---------- ViewModels & Views ----------
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<NotesViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<NotesPage>();

        return builder.Build();
    }
}
