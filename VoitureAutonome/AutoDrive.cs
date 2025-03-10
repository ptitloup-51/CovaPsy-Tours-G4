using RpLidar.NET;
using RpLidar.NET.Entities;

namespace VoitureAutonome;

public class AutoDrive
{
    private bool isRunning;
    
    // Dictionnaire pour stocker l'angle, la distance et le timestamp de mise à jour
    private static Dictionary<int, (float distance, DateTime lastUpdated)> lidarData = new();
    private static int lastPoint;

    private Thrust thrust;
    private Steering steering;
    
    
    public AutoDrive()
    {
        isRunning = false;
    }

    public void Dispose()
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
        
        var PORT_NAME = "/dev/ttyUSB0";

        Console.WriteLine("Starting RpLidar Test");
        var lidar = new RPLidar(PORT_NAME, 256000);
        lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
        Console.WriteLine("Lidar connected");

        thrust = new Thrust();
        steering = new Steering();
        steering.SetDirection(0);

        Thread.Sleep(3000);
        thrust.SetSpeed(1);
        
        Console.CancelKeyPress += (sender, e) => { isRunning = false; };

        while (isRunning) steering.SetDirection(MapValue(GetAngle(350), 0, 180, -90, 90));

        lidar.Dispose();
        thrust.Dispose();
        steering.Dispose();
    }
    
    private static int MapValue(int x, int inMin, int inMax, int outMin, int outMax)
    {
        return outMin + (x - inMin) * (outMax - outMin) / (inMax - inMin);
    }

    private static void Lidar_LidarPointScanEvent(List<LidarPoint> points)
    {
        foreach (var point in points)
        {
            var originalAngle = (int)Math.Round(point.Angle); // Conversion en entier
            var distance = point.Distance;

            // Filtrer les angles de 90° à 270°
            if (originalAngle >= 90 && originalAngle <= 270)
                continue;

            // Transformer l'angle : 360° → 90°, 90° → 0°, 270° → 180°
            var adjustedAngle = (originalAngle + 90) % 360;

            // Mettre à jour le dictionnaire avec le timestamp
            lidarData[adjustedAngle] = (distance, DateTime.Now);
        }
    }


    private static int GetAngle(int ms, int zone = 10)
    {
        var angles = new Dictionary<int, float>();
        
        for (var i = 0; i < 180; i += zone) // de 0 à 180
        {
            var count = 0;
            float distance = 0;

            for (var j = 0; j < zone; j++) // par pas de 10
                if (lidarData.ContainsKey(i + j))
                    if (DateTime.Now - lidarData[i + j].lastUpdated < TimeSpan.FromMilliseconds(ms))
                    {
                        distance += lidarData[i + j].distance;
                        count++;
                    }

            angles.Add(i, distance / count); // on fait la moyenne
        }

        var maxAngle = -1;
        float maxDistance = 0;

            foreach (var angle in angles)
            if (angle.Value > maxDistance)
            {
                maxAngle = angle.Key;

                maxDistance = angle.Value;
            }


        if (maxAngle != -1)
        {
            lastPoint = maxAngle;
            return maxAngle;
        }

        //  Console.WriteLine("retourning last angle");
        return lastPoint;
    }
    

}