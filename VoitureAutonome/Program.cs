using System.Net.Http.Headers;
using RpLidar.NET;
using RpLidar.NET.Entities;
using VoitureAutonome;

public class Program
{

    private static AutoDriveV3 auto;
    static Steering steering = new();
    
    static SteeringTest test = new();
    
    private static void Main(string[] args)
    {
        Misc misc = new();
        misc.Test();
        
       // test.Test();
        
        RemoteDebug debug = new();
        
        
        
        auto = new AutoDriveV3();
      
        debug.CommandCallback += HandleCommande;
        
        Thread.Sleep(Timeout.Infinite);
        
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
                auto.Radius = Convert.ToInt32(content);
                break;
            default:
                Console.WriteLine("commande inconnue " + command);
                break;
        }
    }

}