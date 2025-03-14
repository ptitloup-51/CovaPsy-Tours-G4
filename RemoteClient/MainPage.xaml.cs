namespace RemoteClient;

public partial class MainPage : ContentPage
{
    private Listener Listener;

    public MainPage()
    {
        InitializeComponent();
    }

    private void Start_OnClicked(object? sender, EventArgs e)
    {   
        Listener.Send("start");
    }

    private void Update(string command, string value)
    {
        switch (command)
        {
            case "Heure":
                TimeLabel.Text = value;
                break;
            case "vitesse":
                SpeedLabel.Text = value;
                break;
            case "Angle":
                SteeringLabel.Text = value;
                break;
            case " ":
                break;
        }
        
    }

    private void Connect_OnClicked(object? sender, EventArgs e)
    {
        if (Address.Text == null || Port.Text == null)
        {
            Address.Text = "192.168.2.127";
            Port.Text = "4444";
        }
        
        Listener = new Listener(Address.Text, Convert.ToInt32(Port.Text), 1000);
        Listener.NewValues += Update;
        ConnexionBTN.IsEnabled = false;
        ConnexionBTN.Text = "Connecté";
        CommandBox.IsEnabled = true;

       
    }

    private void StopButton_OnClicked(object? sender, EventArgs e)
    {
        Listener.Send("stop");
    }

    private void KillButton_OnClicked(object? sender, EventArgs e)
    {
        Listener.Send("kill");
    }

    private bool canSend = false;
   
/*
    private void SendRadius_OnClicked(object? sender, EventArgs e)
    {
        Listener.Send("radius", RaduisEntry.Text);
    }
    */
}