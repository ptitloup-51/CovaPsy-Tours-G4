using RpLidar.NET;
using RpLidar.NET.Entities;
using VoitureAutonome;

public class Program
{

    private static AutoDrive auto;
    private static void Main(string[] args)
    {

        RemoteDebug debug = new();
        auto = new AutoDrive();
        
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
            default:
                Console.WriteLine("commande inconnue " + command);
                break;
        }
    }

}