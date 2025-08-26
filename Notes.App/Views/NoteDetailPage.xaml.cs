using Notes.App.ViewModels;

namespace Notes.App.Views;
public partial class NoteDetailPage : ContentPage
{
    public NoteDetailPage(NoteDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is NoteDetailViewModel vm) await vm.InitializeAsync();
    }
}
