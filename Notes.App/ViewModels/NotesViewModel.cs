using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Notes.App.Models;
using Notes.App.Services;


namespace Notes.App.ViewModels;
public partial class NotesViewModel : ObservableObject
{
    private readonly LocalDb _db; 
    private readonly SyncService _sync;
    public ObservableCollection<NoteModel> Items { get; } = new();


    public NotesViewModel(LocalDb db, SyncService sync) { _db = db; _sync = sync; }


    [RelayCommand]
    public async Task LoadAsync()
    {
        var list = await _db.GetNotesAsync();
        Items.Clear(); foreach (var n in list) Items.Add(n);
    }


    [RelayCommand]
    public async Task AddAsync()
    {
        var n = new NoteModel { Title = "Nieuwe notitie" };
        await _db.UpsertAsync(n);
        Items.Insert(0, n);
    }


    [RelayCommand]
    public async Task SaveAsync(NoteModel n)
    {
        n.UpdatedAtUtc = DateTime.UtcNow; n.IsDirty = true;
        await _db.UpsertAsync(n);
        await _sync.SyncAsync();
        await LoadAsync();
    }


    [RelayCommand]
    public async Task DeleteAsync(NoteModel n)
    {
        n.IsDeleted = true; n.IsDirty = true;
        await _db.UpsertAsync(n);
        await _sync.SyncAsync();
        await LoadAsync();
    }
}