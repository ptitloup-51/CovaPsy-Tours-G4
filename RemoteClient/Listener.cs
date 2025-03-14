namespace RemoteClient;



public class Listener
{
    public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

    private static readonly HttpClient client = new HttpClient();
    
    private string adress = string.Empty;
    private int port = 0;
    
    string Baseurl; 
    
    public Listener(string _adress, int _port, int UpdateMs)
    {
        
        adress = _adress;
        port = _port;
        
        Baseurl = $"http://{adress}:{port}/";
        
        Values.TryAdd("Heure", "..-..-..");
        Values.TryAdd("Temperature", "0°C");
        Values.TryAdd("vitesse", "xx m/s");
        Values.TryAdd("Status", "not running");
        Values.TryAdd("Angle", "0°");

        GetValues();
    }
    
    public async void Send(string command, string content = "null")
    {
        //Console.WriteLine($"iuheiuzefzef");
        //Console.WriteLine($"appel de la page: http://{adress}:{port}/send;{command};{content}");
        await new HttpClient().GetAsync($"http://{adress}:{port}/send;{command};{content}");
    }
    
    
    
    // Définition du délégué
    public delegate void Callback(string command, string value);

    // Définition de l'événement basé sur ce délégué
    public event Callback? NewValues;
    
    private async void GetValues()
    {
        while (true)
        {
            foreach (var val in Values)
            {
                HttpResponseMessage response = await client.GetAsync(Baseurl + val.Key);
                if (response.IsSuccessStatusCode)
                {
                    string value = await response.Content.ReadAsStringAsync();
                    Values[val.Key] = value;
                   // Console.WriteLine($"{val.Key} vaut {value}");
                    NewValues?.Invoke(val.Key, value);
                }
                else
                {
                    Console.WriteLine($"Erreur: {response.StatusCode}");
                }
            }
            Thread.Sleep(200);
        }
    }
}