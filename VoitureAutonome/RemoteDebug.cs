using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace VoitureAutonome;

/// <summary>
///     Class <c>Permet de communiquer des informations de télémetrie au client</c>
/// </summary>
public class RemoteDebug
{
    public string Vitesse { get; set; } = "xx m/s";
    public bool IsRunning;
    public RemoteDebug()
    {
        IsRunning = true;
        Task.Run(() => RunServer());
    }

    public void Dispose()
    {
        IsRunning = false;
    }

    
    /// <summary>
    /// Permet d'obtenir l'adresse ip local, retourne l'adresse de la première interface étant connecté à internet
    /// </summary>
    /// <returns></returns>
    private static string GetLocalIPAddress()
    {
        foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            if (netInterface.OperationalStatus == OperationalStatus.Up &&
                netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                foreach (var ip in netInterface.GetIPProperties().UnicastAddresses)
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        return ip.Address.ToString(); // Retourne la première adresse IPv4 trouvée
        return "127.0.0.1"; // Fallback si aucune IP trouvée
    }


    /// <summary>
    /// Execute le serveur Web pour les requetes API
    /// </summary>
    /// <param name="address"></param>
    /// <param name="port"></param>
    public void RunServer(string address = "127.0.0.1", int port = 4444)
    {
        address = GetLocalIPAddress();
        Console.WriteLine($"Starting server on {address}:{port}");
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://{address}:{port}/");
        listener.Start();
        Console.WriteLine($"Serveur démarré sur http://{address}:{port}/");

        while (true) // IsRunning
        {
            var context = listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "GET")
            {
                HandleGetRequest(context);
            }
            else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/post-value")
            {
                HandlePostRequest(context);
            }

            response.OutputStream.Close();
        }
        Console.WriteLine("Serveur arreté");
    }
    
    
    //Gestion du callback
    public delegate void Callaback(string command, string content);
    
    public event Callaback? CommandCallback;

    /// <summary>
    /// Gestion des requetes GET
    /// </summary>
    /// <param name="context"></param>
    private void HandleGetRequest(HttpListenerContext context)
    {
       
        if (context.Request.Url.AbsolutePath.StartsWith("/send"))
        {
            Console.WriteLine($"Command request from {context.Request.RemoteEndPoint}");
            Console.WriteLine($"Traitement de la commande");
            string[] content = context.Request.Url.AbsolutePath.Split(';');
            CommandCallback?.Invoke(content[1], content[2]);
            Console.WriteLine($"Commande : {content[1]}, content : {content[2]}");
        }
        else
        {
            var responseString = "null";
            switch (context.Request.Url.AbsolutePath)
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
                case "/Status":
                    responseString = IsRunning? "yes" : "no";
                    break;
                case "/angle":
                    responseString = "null";
                    break;
                case "/vitesse":
                    responseString = Vitesse;
                    break;
                default:
                    responseString = "pas de valeur";
                    break;
            }

            var buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        
        
    }
    
    /// <summary>
    /// Gestion des requestes post
    /// </summary>
    /// <param name="context"></param>
    private static void HandlePostRequest(HttpListenerContext context)
    {
        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        var requestBody = reader.ReadToEnd();

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);
            if (data == null || !data.ContainsKey("commande"))
            {
                SendResponse(context.Response, "Format JSON invalide", HttpStatusCode.BadRequest);
                return;
            }

            string command = data["commande"];
            Console.WriteLine(command);

            Console.WriteLine($"Commande reçue: {command}");
            SendResponse(context.Response, $"Résultat: {"ok"}", HttpStatusCode.OK);
        }
        catch (JsonException)
        {
            SendResponse(context.Response, "Erreur de décodage JSON", HttpStatusCode.BadRequest);
        }
    }
    
    
    /// <summary>
    /// Permet d'envoyer un message au client
    /// </summary>
    /// <param name="response"></param>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    private static void SendResponse(HttpListenerResponse response, string message, HttpStatusCode statusCode)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.StatusCode = (int)statusCode;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
    
    
}