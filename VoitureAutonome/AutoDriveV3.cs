using RpLidar.NET;
using RpLidar.NET.Entities;

namespace VoitureAutonome;


class Gap
{
    public int start { get; set; }
    public int end { get; set; }

    //retourne la longueur du gap
    public int GetGapLengh()
    {
        return end - start;
    }

    //retourne l'angle médian du gap
    public float GetMid()
    {
        return (start + end) / 2;
    }
}

public class AutoDriveV3
{
    private bool isRunning;
    private float[] LidarPoints = new float[180];
    Misc Misc = new Misc();
    Steering Steering = new Steering();
    Thrust Thrust = new Thrust();

    public float Radius = 1000.0f;

    public AutoDriveV3()
    {
        isRunning = false;
    }

    public void Stop()
    {
        isRunning = false;
    }

    public void Run()
    {
        isRunning = true;
        
        var lidar = new RPLidar("/dev/ttyUSB0", 256000);
        Steering.SetDirection(0);
        Thread.Sleep(5000);
        lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
        Thrust.SetSpeed(8);
        
        
        
        while (isRunning)
        {
        //    Console.WriteLine("Follow the gap angle : " + FollowTheGap());
        Steering.SetDirection(Misc.LinearMap((int)FollowTheGap(), 0, 180, -100, 100));
        }
        
        Thrust.Dispose();
        Steering.Dispose();
        lidar.Dispose();
    }

    public float FollowTheGap()
    {
        List<Gap> gaps = new List<Gap>();

        int start = 0;
        int end = 0;

        for (int i = 0; i < 180; i++)
        {
            if (LidarPoints[i] < Radius) // bulle de sécurité
            {
                LidarPoints[i] = 0;
                gaps.Add(new Gap { start = start, end = end });
                end = i + 1;
                start = i + 1;
            }
            else
            {
                end++;
            }
        }

        /*
        if (gaps.Count == 0) // s'il n'y a pas de gap
        {
            gaps.Add(new Gap { start = 0, end = 180 });
            Console.WriteLine("pas de GAP");
        }
        */
        
        Gap bestGap = new Gap{ start = 0, end = 0 };

        foreach (Gap gap in gaps) //on parcours tous les espaces remplis
        {
            if (gap.GetGapLengh() > bestGap.GetGapLengh()) // on garde le meilleur gap, le plus large
            {
                bestGap = gap;
            }
        }

        if (bestGap.GetGapLengh() == 0)
        {
            bestGap.start = 0;
            bestGap.end = 180;
        }
        
        
        
        
        
        Console.WriteLine("------------");
        Console.WriteLine("start gap : " + bestGap.start);
        Console.WriteLine("mid gap : " + bestGap.GetMid());
        Console.WriteLine("end gap : " + bestGap.end);
        Console.WriteLine("Gap lenght : " + bestGap.GetGapLengh());
        Console.WriteLine("------------");
        
        
        
        
        return bestGap.GetMid(); //on retourne la valeur du milieu de l'espace
    }
    
    
    
    // Stockage global des valeurs précédentes (mémoire du dernier scan)
    private float[] lidarMemory360 = new float[360];  
    
    DateTime LastTime = DateTime.Now;
    private void Lidar_LidarPointScanEvent(List<LidarPoint> points)
    {
        
        
        float[] rawData180 = new float[180];

        // Mettre à jour uniquement les angles reçus
        foreach (var point in points)
        {
            if (point.Quality > 10) // Si le point est de qualité suffisante
            {
                int angleIndex = (int)Math.Round(point.Angle) % 360;
                lidarMemory360[angleIndex] = point.Distance; // on l'ajoute
            }
        }
        
        // Transformer les 360° en 180° en replaçant les valeurs correctement
        for (int i = 0; i < 180; i++)
        {
            int lidarAngle = (i - 90 + 360) % 360; // Décalage pour centrer sur 90°
            rawData180[i] = lidarMemory360[lidarAngle];
        }
        
       // Console.WriteLine($"angle 90° → {Misc.KalmanFilter(rawData180)[90]}");

        // Appliquer le filtre de Kalman
        LidarPoints = Misc.KalmanFilter(rawData180);
        
        
       // Console.WriteLine("dernière update : " + (DateTime.Now - LastTime).TotalMilliseconds); //environ 420ms
        LastTime = DateTime.Now;
    }
    
}