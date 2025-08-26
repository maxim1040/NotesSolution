using Notes.App.ViewModels;

namespace Notes.App.Views;
public partial class NotesPage : ContentPage
{
    public NotesPage(NotesViewModel vm)
    {
        InitializeComponent(); 
        BindingContext = vm;
    }
}