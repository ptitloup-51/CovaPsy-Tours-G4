namespace RemoteClient;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void Start_OnClicked(object? sender, EventArgs e)
    {
        Listener listener = new Listener();
    }
}