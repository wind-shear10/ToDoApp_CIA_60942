namespace ToDoApp;
public partial class App : Application
{
    private readonly MainPage _page;
    public App(MainPage page)
    {
        InitializeComponent();
        _page = page;
        UserAppTheme = AppTheme.Light;
    }
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(_page) { Title = "Мои задачи", Width = 1100, Height = 760, MinimumWidth = 800, MinimumHeight = 600 };
}
