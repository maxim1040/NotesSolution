using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Notes.App.Models;
using Notes.App.Services;

namespace Notes.App.ViewModels;

public partial class NotesViewModel : ObservableObject
{
    private readonly LocalDb _db;

    public ObservableCollection<NoteModel> Items { get; } = new();

    [ObservableProperty] private NoteModel? selected;
    [ObservableProperty] private bool isBusy;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand NewNoteCommand { get; }
    public IAsyncRelayCommand<NoteModel?> OpenNoteCommand { get; }

    public NotesViewModel(LocalDb db)
    {
        _db = db;
        RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        NewNoteCommand = new AsyncRelayCommand(NewAsync, () => !IsBusy);
        OpenNoteCommand = new AsyncRelayCommand<NoteModel?>(OpenAsync, _ => !IsBusy);
    }

    private void UpdateCanExec()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        NewNoteCommand.NotifyCanExecuteChanged();
        OpenNoteCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadAsync()
    {
        if (IsBusy) return; IsBusy = true; UpdateCanExec();
        try
        {
            var list = await _db.GetNotesAsync();
            Items.Clear();
            foreach (var n in list) Items.Add(n);
        }
        finally { IsBusy = false; UpdateCanExec(); }
    }

    private Task NewAsync() =>
        Shell.Current.GoToAsync("noteDetail"); // geen id => nieuwe note

    private async Task OpenAsync(NoteModel? item)
    {
        if (item == null) return;
        Selected = null; // clear UI selection
        await Shell.Current.GoToAsync($"noteDetail?id={item.Id}");
    }
}
