using System.Net.Http.Headers;
using RpLidar.NET;
using RpLidar.NET.Entities;
using VoitureAutonome;

public class Program
{

    private static AutodriveV2 auto;
    static Steering steering = new();
    
    static SteeringTest test = new();
    
    static STMCom stcom = new ();

    private static RemoteDebug debug = new();
    
    private static void Main(string[] args)
    {
        Misc misc = new();
        misc.Test();
        
        stcom.OnMessageReceive += StcomOnOnMessageReceive;
        
       // test.Test();
       
        auto = new();
      
        debug.CommandCallback += HandleCommande;
        
        Thread.Sleep(Timeout.Infinite);
        
    }

    private static void StcomOnOnMessageReceive(string message)
    {
        debug.Vitesse = message;
    }


    private static void HandleCommande(string command, string content)
    {
        switch (command)
        {
            case "kill": //tue le processus
                Console.WriteLine("fin du programme !");
                System.Environment.Exit(0);
                break;
            case "start": //démarre la conduite
                Console.WriteLine("début de la conduite !!");
                new Thread(() => auto.Run()).Start();
                break;
            case "stop": //arrete la voiture
                Console.WriteLine("Fin de la conduite !!");
                auto.Stop();
                break;
            case "steer":
                steering.SetDirection(Convert.ToInt32(content));
                break;
            case "radius":
                Console.WriteLine("nouveau radius : " + content);
               // auto.Radius = Convert.ToInt32(content);
                break;
            case "scan":
                ScanTest(Convert.ToInt32(content));
                break;
            default:
                Console.WriteLine("commande inconnue " + command);
                break;
        }
    }

    static void ScanTest(int time)
    {
        var lidar = new RPLidar("/dev/ttyUSB0", 256000);
        lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
        Thread.Sleep(time * 1000); // Attendre que le LIDAR soit prêt
        lidar.Dispose();
    }

    private static void Lidar_LidarPointScanEvent(List<LidarPoint> points)
    {
       
        foreach (var point in points)
        {
           Console.WriteLine(point.ToString());
        }
        
    }
}