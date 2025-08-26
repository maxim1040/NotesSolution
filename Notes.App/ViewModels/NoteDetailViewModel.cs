using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Notes.App.Models;
using Notes.App.Services;

namespace Notes.App.ViewModels;


[QueryProperty(nameof(NoteId), "id")]
public partial class NoteDetailViewModel : ObservableObject
{
    private readonly LocalDb _db;
    private readonly SyncService _sync;

    private NoteModel? _entity;

    [ObservableProperty] private string pageTitle = "Nieuwe notitie";
    [ObservableProperty] private string title = "";
    [ObservableProperty] private string content = "";

    public string? NoteId { get; set; }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }

    public NoteDetailViewModel(LocalDb db, SyncService sync)
    {
        _db = db; _sync = sync;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync);
        CancelCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync(".."));
    }

    public async Task InitializeAsync()
    {
        // Parse de string naar Guid als er een id is
        if (!string.IsNullOrWhiteSpace(NoteId) && Guid.TryParse(NoteId, out var id))
        {
            _entity = await _db.GetAsync(id);
            if (_entity != null)
            {
                Title = _entity.Title;
                Content = _entity.Content;
                PageTitle = "Notitie bewerken";
            }
        }
    }

    private async Task SaveAsync()
    {
        if (_entity == null)
        {
            _entity = new NoteModel
            {
                Title = Title,
                Content = Content,
                IsDirty = true
            };
        }
        else
        {
            _entity.Title = Title;
            _entity.Content = Content;
            _entity.IsDirty = true;
            _entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.UpsertAsync(_entity);
        await _sync.SyncAsync();
        await Shell.Current.GoToAsync("..");
    }

    private async Task DeleteAsync()
    {
        if (_entity == null) { await Shell.Current.GoToAsync(".."); return; }
        _entity.IsDeleted = true; _entity.IsDirty = true;
        await _db.UpsertAsync(_entity);
        await _sync.SyncAsync();
        await Shell.Current.GoToAsync("..");
    }
}
