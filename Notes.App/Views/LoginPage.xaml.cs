using Notes.App.ViewModels;

namespace Notes.App.Views;
public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent(); 
        BindingContext = vm;
    }
}