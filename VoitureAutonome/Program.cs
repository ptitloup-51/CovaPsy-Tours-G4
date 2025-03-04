using VoitureAutonome;
using System;
using System.Diagnostics;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        /*
        Thrust th = new Thrust();
        Thread.Sleep(5000);
        th.SetSpeed(20);
        Thread.Sleep(5000);
        th.SetSpeed(40);
        Thread.Sleep(5000);
        th.Dispose();
        */
         
        
        
        // Créer une instance de ContinuousLidar
        var lidar = new ContinuousLidar();

        // Démarrer le scan continu
        lidar.StartContinuousScan();
        Thread.Sleep(4000);
        
        
        
        for (int i = 0; i < 20; i++)
        {
            // Prendre une photo quand vous le souhaitez
            lidar.TakePhoto($"ma_photo{i}");
        }
        
        Console.WriteLine($"Temps moyen: {lidar.TotalTime / lidar.count}");

        // Obtenir la distance à un angle spécifique
        Console.WriteLine(lidar.GetDistanceAtAngle(0)); // Distance à 90 degrés

        // Arrêter le scan quand vous avez terminé
        lidar.StopContinuousScan();
       
        
    }
}