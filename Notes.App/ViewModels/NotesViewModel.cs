using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Notes.App.Models;
using Notes.App.Services;
using Notes.App.Views;

namespace Notes.App.ViewModels;

public partial class NotesViewModel : ObservableObject
{
    private readonly LocalDb _db;
    private readonly SyncService _sync;
    private readonly AuthService _auth;
    private readonly IServiceProvider _sp;

    public ObservableCollection<NoteModel> Items { get; } = new();

    [ObservableProperty] private bool isBusy;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand NewNoteCommand { get; }
    public IAsyncRelayCommand<NoteModel?> OpenNoteCommand { get; }
    public IAsyncRelayCommand<NoteModel?> DeleteFromListCommand { get; }
    public IAsyncRelayCommand LogoutCommand { get; }

    public NotesViewModel(LocalDb db, SyncService sync, AuthService auth, IServiceProvider sp)
    {
        _db = db; _sync = sync; _auth = auth; _sp = sp;

        RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        NewNoteCommand = new AsyncRelayCommand(NewAsync, () => !IsBusy);
        OpenNoteCommand = new AsyncRelayCommand<NoteModel?>(OpenAsync, _ => !IsBusy);
        DeleteFromListCommand = new AsyncRelayCommand<NoteModel?>(DeleteAsync, _ => !IsBusy);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync, () => !IsBusy);
    }

    private void UpdateCanExec()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        NewNoteCommand.NotifyCanExecuteChanged();
        OpenNoteCommand.NotifyCanExecuteChanged();
        DeleteFromListCommand.NotifyCanExecuteChanged();
        LogoutCommand.NotifyCanExecuteChanged();
    }


    private async Task LoadAsync()
    {
        if (IsBusy) return; IsBusy = true; UpdateCanExec();
        try
        {

            var token = await SecureStorage.GetAsync(Constants.AccessTokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                Application.Current.MainPage = _sp.GetRequiredService<LoginPage>();
                return;
            }

            if (!_db.IsReady)
            {
                var uid = await _auth.EnsureUserIdAsync();
                if (!string.IsNullOrWhiteSpace(uid))
                    await _db.UseUserAsync(uid);
                else
                {
                    Application.Current.MainPage = _sp.GetRequiredService<LoginPage>();
                    return;
                }
            }

            var list = await _db.GetNotesAsync();
            Items.Clear();
            foreach (var n in list) Items.Add(n);

            try { await _sync.SyncAsync(); }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                await _auth.LogoutAsync();
                Application.Current.MainPage = _sp.GetRequiredService<LoginPage>();
            }
        }
        finally { IsBusy = false; UpdateCanExec(); }
    }

    private Task NewAsync() => Shell.Current.GoToAsync("noteDetail");

    private async Task OpenAsync(NoteModel? item)
    {
        if (item == null) return;
        await Shell.Current.GoToAsync($"noteDetail?id={item.Id}");
    }

    private async Task DeleteAsync(NoteModel? n)
    {
        if (n == null || IsBusy) return;
        IsBusy = true; UpdateCanExec();
        try
        {
            n.IsDeleted = true; n.IsDirty = true;
            await _db.UpsertAsync(n);
            await _sync.SyncAsync();
            Items.Remove(n);
        }
        finally { IsBusy = false; UpdateCanExec(); }
    }

    private async Task LogoutAsync()
    {
        if (IsBusy) return; IsBusy = true; UpdateCanExec();
        try
        {
            _db.Reset();
            await _auth.LogoutAsync();
            Application.Current.MainPage = _sp.GetRequiredService<LoginPage>();
        }
        finally { IsBusy = false; UpdateCanExec(); }
    }
}
