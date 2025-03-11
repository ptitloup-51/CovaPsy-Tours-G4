using System.Diagnostics;
using RpLidar.NET;
using RpLidar.NET.Entities;

namespace VoitureAutonome;

public class AutodriveV2
{
    private bool IsRunning;
    private float[] LidarPoints = new float[180];
    Misc Misc = new Misc();
    Steering Steering = new Steering();
    Thrust Thrust = new Thrust();
    

    public AutodriveV2()
    { 
        IsRunning = false;
    }

    public int FindLongestPath()
    {
        int longestAngle = 0;
        float longestDistance = 0;
        for (int i = 0; i < 180; i++)
        {
            if (LidarPoints[i] > longestDistance)
            {
                longestAngle = i;
                longestDistance = LidarPoints[i];
            }
        }
        
        return longestAngle;
    }
    
    public int FindBestDirection()
    {
        int bestAngle = 90; // Par défaut, avancer tout droit
        float maxGapSize = 0;
        int currentGapStart = -1;
        int currentGapSize = 0;
    
        float minSafeDistance = 300f; // Seuil de distance acceptable pour avancer

        for (int i = 0; i < LidarPoints.Length; i++)
        {
            if (LidarPoints[i] > minSafeDistance) // On considère qu'il y a un passage libre
            {
                if (currentGapStart == -1)
                    currentGapStart = i;

                currentGapSize++;
            }
            else
            {
                if (currentGapSize > maxGapSize)
                {
                    maxGapSize = currentGapSize;
                    bestAngle = currentGapStart + currentGapSize / 2; // Centre du plus grand gap
                }

                currentGapStart = -1;
                currentGapSize = 0;
            }
        }

        // Vérification si le dernier gap est le plus grand
        if (currentGapSize > maxGapSize)
        {
            bestAngle = currentGapStart + currentGapSize / 2;
        }

        // Correction pour éviter des valeurs en dehors de [0,180]
        bestAngle = Math.Clamp(bestAngle, 0, 180);
    
        Console.WriteLine($"Best Angle: {bestAngle}");
        return bestAngle;
    }

    public void Run()
    {
        IsRunning = true;
        
        var lidar = new RPLidar("/dev/ttyUSB0", 256000);
        Steering.SetDirection(0);
        Thread.Sleep(4000);
        lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
        Thrust.SetSpeed(5);
        
        int bestangle = 90;
        while (IsRunning)
        {
            bestangle = FindLongestPath();
            Console.WriteLine(Misc.MapValue(bestangle, 0, 180, -100, 100));
            Steering.SetDirection(Misc.MapValue(bestangle, 0, 180, -100, 100));
           // Console.WriteLine(bestangle); 
        }
        
        
        Thrust.Dispose();
        Steering.Dispose();
        lidar.Dispose();
        
        
    }
    
    // Stockage global des valeurs précédentes (mémoire du dernier scan)
    private float[] lidarMemory360 = new float[360];  
    
    private void Lidar_LidarPointScanEvent(List<LidarPoint> points)
    {
        float[] rawData180 = new float[180];

        // Mettre à jour uniquement les angles reçus
        foreach (var point in points)
        {
            int angleIndex = (int)Math.Round(point.Angle) % 360;
           
                lidarMemory360[angleIndex] = point.Distance;
            
        }

   

        // Transformer les 360° en 180° en replaçant les valeurs correctement
        for (int i = 0; i < 180; i++)
        {
            int lidarAngle = (i - 90 + 360) % 360; // Décalage pour centrer sur 90°
            rawData180[i] = lidarMemory360[lidarAngle];

            
        }

    
        
      //  Console.WriteLine($"angle 90°) → {Misc.KalmanFilter(rawData180)[90]}");

        // Appliquer le filtre de Kalman
        LidarPoints = rawData180;
    }
    
    
    public void Stop()
    {
        IsRunning = false;
    }
    
}