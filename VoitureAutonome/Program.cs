using VoitureAutonome;
using System;
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
        
        
        
        // Créer le répertoire pour les images si nécessaire
        string imageDir = "/home/covapsytours5/Documents/images/";
        Directory.CreateDirectory(imageDir);
        
        Console.WriteLine("Démarrage du système de cartographie LIDAR...");
        
        // Durée d'exécution (en secondes) - à ajuster selon vos besoins
        int runDuration = 10;
        
        using (var lidar = new ContinuousLidar())
        {
            try
            {
                // Démarrer la cartographie continue avec:
                // - Prise de mesures toutes les 100ms
                // - Génération d'images toutes les 500ms
                lidar.StartContinuousMapping(scanIntervalMs: 100, imageIntervalMs: 500);
                
                Console.WriteLine($"Cartographie en cours pour {runDuration} secondes...");
                
                // Exécuter pendant une durée fixe au lieu d'attendre une entrée console
                // Solution pour éviter l'erreur Console.ReadKey
                for (int i = 0; i < runDuration; i++)
                {
                    Thread.Sleep(1000);
                    if (i % 5 == 0)
                    {
                        Console.WriteLine($"Temps écoulé: {i} secondes");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
            }
            finally
            {
                // S'assurer que le lidar est correctement arrêté
                lidar.StopContinuousMapping();
                Console.WriteLine("Système de cartographie arrêté.");
            }
        }
        
    }
}