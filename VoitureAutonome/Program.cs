using RpLidar.NET;
using RpLidar.NET.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VoitureAutonome;

public class Program
{
    static void Main(string[] args)
    {
        /*
        Steering steering = new Steering();
        steering.Center();
        Thread.Sleep(4000);
        steering.SetDirection(18);
        Thread.Sleep(4000);
        steering.SetDirection(-18);
        Thread.Sleep(4000);
        steering.Center();
        */
        
        
        
        string PORT_NAME = "/dev/ttyUSB0";

        Console.WriteLine("Starting RpLidar Test");
        RPLidar lidar = new RPLidar(PORT_NAME, 256000);
        lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
        Console.WriteLine("Lidar connected");

        Thrust thrust = new Thrust();
        Steering steering = new Steering();
        
        steering.Center();

        Thread.Sleep(3000);
        thrust.SetSpeed(1); // Démarrer la voiture

        bool isRunning = true;
        Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
        {
            isRunning = false;
        };

        while (isRunning)
        {

            // 🔥 Trouver l'angle avec la plus grande distance
           
            var bestAngle = GetBestAngle();
            if (bestAngle.HasValue)
            {
                thrust.SetSpeed(1);
                int maxAngle = bestAngle.Value.angle;
                float maxDistance = bestAngle.Value.distance;
                int error = 90 - maxAngle; // Différence entre le centre et l'angle optimal
                
              //  Console.WriteLine($"Meilleure direction : {maxAngle}° (Distance : {maxDistance} cm)");
              //  Console.WriteLine($"Erreur d'angle : {error}");

                // ➡ Ajuster la direction en fonction de l'erreur
                steering.SetDirection(MapValue(error, -90, 90, 100, -100));
            }
            else
            {
                thrust.Stop();
                Console.WriteLine("Aucune donnée valide pour déterminer la direction.");
            }
        }

        lidar.Dispose();
        
    }

    // ⚡ Fonction de mappage (convertir l'erreur en direction moteur)
    static int MapValue(int x, int inMin, int inMax, int outMin, int outMax)
    {
        return outMin + (x - inMin) * (outMax - outMin) / (inMax - inMin);
    }

    // Dictionnaire pour stocker l'angle, la distance et le timestamp de mise à jour
    public static Dictionary<int, (float distance, DateTime lastUpdated)> lidarData = new Dictionary<int, (float, DateTime)>();

    public static void Lidar_LidarPointScanEvent(List<LidarPoint> points)
    {
        foreach (LidarPoint point in points)
        {
            int originalAngle = (int)Math.Round(point.Angle); // Conversion en entier
            float distance = point.Distance;

            // Filtrer les angles de 90° à 270°
            if (originalAngle >= 90 && originalAngle <= 270)
                continue;

            // Transformer l'angle : 360° → 90°, 90° → 0°, 270° → 180°
            int adjustedAngle = (originalAngle + 90) % 360;

            // Mettre à jour le dictionnaire avec le timestamp
            lidarData[adjustedAngle] = (distance, DateTime.Now);
        }
    }

    // 🔥 Trouver l'angle avec la plus grande distance
    public static (int angle, float distance)? GetBestAngle()
    {
        Dictionary<int, float> smoothedData = new Dictionary<int, float>();

        // Grouper les mesures par tranches de 5°
        for (int angle = 0; angle < 180; angle += 5)
        {
            float totalDistance = 0;
            int count = 0;

            for (int offset = -2; offset <= 2; offset++) // Moyenne sur ±2°
            {
                int checkAngle = angle + offset;
                if (lidarData.TryGetValue(checkAngle, out var value))
                {
                    totalDistance += value.distance;
                    count++;
                }
            }

            if (count > 0)
            {
                smoothedData[angle] = totalDistance / count; // Moyenne des distances
            }
        }

        // Trouver l'angle avec la plus grande distance
        if (smoothedData.Count > 0)
        {
            var maxEntry = smoothedData.Aggregate((l, r) => l.Value > r.Value ? l : r);
            return (maxEntry.Key, maxEntry.Value);
        }

        return null;
    }
}
