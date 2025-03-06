using RpLidar.NET;
using RpLidar.NET.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using VoitureAutonome;

public class Program
{
    static void Main(string[] args)
    {
        
        /*
        TestSpeed speed = new TestSpeed();
        speed.Speed();
        */
        
        
        
        string PORT_NAME = "/dev/ttyUSB0";

        Console.WriteLine("Starting RpLidar Test");
        RPLidar lidar = new RPLidar(PORT_NAME, 256000);
        lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
        Console.WriteLine("Lidar connected");

        Thrust thrust = new Thrust();
        Steering steering = new Steering();
        steering.SetDirection(0);
        
        Thread.Sleep(3000);
        thrust.SetSpeed(5);
        
        
        

        bool isRunning = true;
        Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
        {
            isRunning = false;
        };

        while (isRunning)
        {
           // Thread.Sleep(100); // Rafraîchissement rapide (500ms)

          //  var recentData = GetRecentDistance(75, 5); // Cherche autour de 75° (±3°)

          //double value = GetAngle2();
          //Console.WriteLine("Rapport: " + value);

          /*
          if (value < 0)
          {
              steering.SetDirection(100);
          }
          else
          {
              steering.SetDirection(-100);
          }
          */

          //   Thread.Sleep(1000);
          //   Console.WriteLine(GetAngle2());

          // Console.WriteLine( " jhzbefjhbzef : " + MapValue(GetRecentMaxDistance(200), 0, 180, -100, 100));




          steering.SetDirection(MapValue(GetAngle(350), 0, 180, -90, 90));

          /*
          if (recentData.HasValue)
          {
            //  Console.WriteLine($"Distance récente autour de 75° : {recentData.Value.distance} cm, Dernière mise à jour : {recentData.Value.timeSinceUpdate.TotalMilliseconds} ms");
            steering.SetDirection(MapValue(GetMaxDistance(), -90, 90, 100, -100));

          }
          else
          {
              Console.WriteLine("Aucune donnée récente autour de 75°.");
          }
          */

        }

        lidar.Dispose();
        
    }
    
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

    
    
    //Retourne l'angle avec la plus grande distance mesurée et qui est plus récent que x millisecondes

    public static double GetAngle2()
    {
        double error = 0;

        int countLeft = 0;
        int countRight = 0;
        float Right = 0;
        float Left = 0;
        
        for (int i = 0; i < 20; i++)
        {
            if (lidarData.ContainsKey(0 + i))
            {
                Right += lidarData[0 + i].distance;
                countRight++;
            }

            if (lidarData.ContainsKey(180 - i))
            {
                Left += lidarData[180 - i].distance;
                countLeft++;
            }
        }
        
       // Console.WriteLine($"Right : {Right}, Left : {Left}, CountLeft: {countLeft}, CountRight: {countRight}");
        
        error = (Right/countRight) / (Left/countLeft);
        return error;
    }
    
    public static int GetAngle(int ms)
    {
        Dictionary<int, float> angles = new Dictionary<int, float>();

        
        
        for (int i = 0; i < 180; i+= 10) // de 0 à 180
        {
            int count = 0;
            float distance = 0;
            
            for (int j = 0; j < 10; j++) // par pas de 10
            {
                if (lidarData.ContainsKey(i + j))
                {
                    if (DateTime.Now - lidarData[i+j].lastUpdated < TimeSpan.FromMilliseconds(ms))
                    {
                   
                        distance += lidarData[i+j].distance;
                        count++;
                        
                    }
                }
                
            }
            angles.Add(i, distance/count); // on fait la moyenne
        }

        int maxAngle = -1;
        float maxDistance = 0;
        
        foreach (KeyValuePair<int, float> angle in angles)
        {
            if (angle.Value > maxDistance)
            {
                maxAngle = angle.Key;
                
                maxDistance = angle.Value;
            }
        }

        if (maxAngle != -1)
        {
            lastPoint = maxAngle;
            return maxAngle;
        }
        else
        {
            return lastPoint;
        }
       
       
    }
    
    
    private static int lastPoint = 0;
    public static int GetRecentMaxDistance(int ms)
    {
        if (lidarData.Count == 0)
        {
            return 0;
        }
        
        float maxDistance = -1;
        int maxAngle = -1;
        //GetRecentDistance(75, 5);

        for (int i = 0; i < 180; i++)
        {
            if (lidarData.ContainsKey(i) && lidarData[i].distance > maxDistance && DateTime.Now - lidarData[i].lastUpdated < TimeSpan.FromMilliseconds(ms))
            {
                maxDistance = lidarData[i].distance;
                maxAngle = i;
            }
        }


        if (maxAngle == -1)
        {
           // Console.WriteLine("pas de max angle");
            return lastPoint;
        }
        
        Console.WriteLine($"Best angle : {maxAngle} with a distance : {maxDistance} update time : {DateTime.Now - lidarData[maxAngle].lastUpdated}");
        lastPoint = maxAngle;
        return maxAngle;
    }

    // 🔥 Cherche la distance la plus récente autour d'un angle donné (ex: 75° ± 3°)
    public static (float distance, TimeSpan timeSinceUpdate)? GetRecentDistance(int targetAngle, int tolerance)
    {
        DateTime latestTime = DateTime.MinValue;
        float? bestDistance = null;
        TimeSpan? bestTimeSinceUpdate = null;

        for (int angle = targetAngle - tolerance; angle <= targetAngle + tolerance; angle++)
        {
            if (lidarData.TryGetValue(angle, out var value))
            {
                TimeSpan timeSinceUpdate = DateTime.Now - value.lastUpdated;
                if (value.lastUpdated > latestTime) // On prend la plus récente
                {
                    latestTime = value.lastUpdated;
                    bestDistance = value.distance;
                    bestTimeSinceUpdate = timeSinceUpdate;
                }
            }
        }

        return bestDistance.HasValue ? (bestDistance.Value, bestTimeSinceUpdate.Value) : null;
    }
}
