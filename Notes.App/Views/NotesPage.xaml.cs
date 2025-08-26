using Notes.App.ViewModels;

namespace Notes.App.Views;
public partial class NotesPage : ContentPage
{
    private readonly NotesViewModel _vm;
    public NotesPage(NotesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshCommand.ExecuteAsync(null);
    }
}
