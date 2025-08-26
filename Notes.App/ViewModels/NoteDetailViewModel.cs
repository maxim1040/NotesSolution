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

    public Guid? NoteId { get; set; }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }

    public NoteDetailViewModel(LocalDb db, SyncService sync)
    {
        _db = db; _sync = sync;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync(".."));
    }

    public async Task InitializeAsync()
    {
        if (NoteId is { } id)
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
}
