namespace RemoteClient;

public partial class MainPage : ContentPage
{
    private Listener listener;

    public MainPage()
    {
        InitializeComponent();
    }

    private void Start_OnClicked(object? sender, EventArgs e)
    {   
        if (Address.Text == null || Port.Text == null)
        {
            Address.Text = "192.168.2.127";
            Port.Text = "5555";
        }
        
        listener = new Listener(Address.Text, Convert.ToInt32(Port.Text));
        
    }
    
}