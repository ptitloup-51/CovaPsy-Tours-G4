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
    static RemoteDebug()
    {
        Task.Run(() => RunServer());
    }

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
    ///     Execute le serveur Web pour les requetes API
    /// </summary>
    /// <param name="address"></param>
    /// <param name="port"></param>
    public static void RunServer(string address = "127.0.0.1", int port = 5555)
    {
        address = GetLocalIPAddress();
        Console.WriteLine($"Starting server on {address}:{port}");
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://{address}:{port}/");
        listener.Start();
        Console.WriteLine($"Serveur démarré sur http://{address}:{port}/");

        while (true)
        {
            var context = listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "GET")
            {
                var responseString = "null";
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

                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/post-value")
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var requestBody = reader.ReadToEnd();
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);

                Console.WriteLine(data["message"]);

                var responseString = $"Received: {data["message"]}";
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            response.OutputStream.Close();
        }
    }
}