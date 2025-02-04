using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace VoitureAutonome;

/// <summary>
/// Class <c>Permet de communiquer des informations de télémetrie au client</c>
/// </summary>
public class RemoteDebug
{
    static RemoteDebug()
    {
        Task.Run(() => RunServer());
    }

    /// <summary>
    /// Execute le serveur Web pour les requetes API
    /// </summary>
    /// <param name="address"></param>
    /// <param name="port"></param>
    public static void RunServer( string address = "127.0.0.1", int port = 5555)
    {
        Console.WriteLine( $"Starting server on {address}:{port}" );
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add($"http://{address}:{port}/");
        listener.Start();
        Console.WriteLine($"Serveur démarré sur http://{address}:{port}/");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            
            if (request.HttpMethod == "GET")
            {
                string responseString = "null";
                switch (request.Url.AbsolutePath)
                {
                    case "/temperature":
                        responseString = "28°C";
                        break;
                    case "/roulie":
                        responseString = "30°";
                        break;
                    case "/Heure":
                        responseString = DateTime.Now.ToLongTimeString();
                        break;
                    default:
                        responseString = "pas de valeur";
                        break;
                }
                
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/post-value")
            {
                using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                string requestBody = reader.ReadToEnd();
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);
                
                Console.WriteLine(data["message"]);
                
                string responseString = $"Received: {data["message"]}";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                
                
            }
            
            response.OutputStream.Close();
        }
    }
    
}