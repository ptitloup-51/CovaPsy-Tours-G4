using System.Diagnostics;

namespace RemoteClient;

public class Listener
{
    public string Time { get; }
    public int Speed { get; }
    public int Angle { get; }

    private static readonly HttpClient client = new HttpClient();
    
    public Listener()
    {
        Debug.WriteLine("Client démarré... Interroge le serveur chaque seconde.");
        Task.Run(() => GetTime());
    }

    private async void GetTime()
    {
        try
        {
            string url = "http://127.0.0.1:5555/Heure";
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string time = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Heure du serveur: {time}");
            }
            else
            {
                Console.WriteLine($"Erreur: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }
    
    

   
}