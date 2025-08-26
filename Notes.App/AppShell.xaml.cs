namespace Notes.App;
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("noteDetail", typeof(Notes.App.Views.NoteDetailPage));
    }
}
