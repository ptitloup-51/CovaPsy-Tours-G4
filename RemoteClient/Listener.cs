using System.Diagnostics;

namespace RemoteClient;

public class Listener
{

    public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

    private static readonly HttpClient client = new HttpClient();
    
    private string adress = string.Empty;
    private int port = 0;
    
    string Baseurl; 
    
    public Listener(string _adress, int _port)
    {
        adress = _adress;
        port = _port;
        
        Baseurl = $"http://{adress}:{port}/";
        
        Values.TryAdd("Heure", "..-..-..");
        Values.TryAdd("Temperature", "0°C");

        GetValues();
    }

    private async void GetValues()
    {
        foreach (var val in Values)
        {
            HttpResponseMessage response = await client.GetAsync(Baseurl + val.Key);
            if (response.IsSuccessStatusCode)
            {
                string value = await response.Content.ReadAsStringAsync();
                Values[val.Key] = value;
                Console.WriteLine($"{val.Key} vaut {value}");
            }
            else
            {
                Console.WriteLine($"Erreur: {response.StatusCode}");
            }
        }
    }
}