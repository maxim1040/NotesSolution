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

        builder.UseMauiApp<App>()
               .ConfigureFonts(f => f.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<LocalDb>();
        builder.Services.AddSingleton<SyncService>();
        builder.Services.AddTransient<TokenHandler>();

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

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<NotesViewModel>();
        builder.Services.AddTransient<NoteDetailViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<NotesPage>();
        builder.Services.AddTransient<NoteDetailPage>();

        return builder.Build();
    }
}
