using RpLidar.NET;
using RpLidar.NET.Entities;
using VoitureAutonome;

public class Program
{
    private static AutodriveV2 auto;
    private static readonly Steering steering = new();

    private static SteeringTest test = new();

    private static readonly STMCom stcom = new();

    private static readonly RemoteDebug debug = new();

    private static void Main(string[] args)
    {
        Misc misc = new();
        misc.Test();

        stcom.OnMessageReceive += StcomOnOnMessageReceive;

        // test.Test();

        auto = new AutodriveV2();

        debug.CommandCallback += HandleCommande;

        Thread.Sleep(Timeout.Infinite);
    }

    private static void StcomOnOnMessageReceive(string message)
    {
        debug.Vitesse = message;
        debug.angle = auto.angleShare;
    }


    private static void HandleCommande(string command, string content)
    {
        switch (command)
        {
            case "kill": //tue le processus
                Console.WriteLine("fin du programme !");
                Environment.Exit(0);
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

    private static void ScanTest(int time)
    {
        var lidar = new RPLidar("/dev/ttyUSB0", 256000);
        lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
        Thread.Sleep(time * 1000);
        lidar.Dispose();
    }

    private static void Lidar_LidarPointScanEvent(List<LidarPoint> points)
    {
        foreach (var point in points) Console.WriteLine(point.ToString());
    }
}