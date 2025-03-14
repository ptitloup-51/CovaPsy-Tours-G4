using RpLidar.NET;
using RpLidar.NET.Entities;

namespace VoitureAutonome;

// Version fonctionnelle avec ralentissement dans les virages

public class AutodriveV2
{
    private bool IsRunning;
    private readonly float[] LidarMemory360 = new float[360]; // Stockage global des valeurs précédentes
    private readonly float[] LidarPoints = new float[180]; //valeurs du lidar 
    private readonly DateTime[] LidarTimestamps360 = new DateTime[360]; // Timestamps pour chaque angle
    private readonly int maxSpeed = 20; // Vitesse maximale
    private readonly int minSpeed = 15; // Vitesse minimale dans les virages
    private readonly Misc Misc = new();

    private readonly float Radius = 1400; //Radius
    private int speed = 20; // Vitesse actuelle
    private readonly Steering Steering = new();
    private readonly Thrust Thrust = new();

    public AutodriveV2()
    {
        IsRunning = false;
        Array.Fill(LidarTimestamps360, DateTime.MinValue); // Initialiser les timestamps des valeurs mesurées par le lidar
    }

    /// <summary>
    /// Algorithme de Follow the Gap
    /// </summary>
    /// <returns></returns>
    public int FindBestDirection()
    {
        var bestAngle = 90; // Par défaut, avancer tout droit
        float maxGapSize = 0;
        var currentGapStart = -1;
        var currentGapSize = 0;

        for (var i = 0; i < LidarPoints.Length; i++)
            if (LidarPoints[i] > Radius) // On considère qu'il y a un passage libre
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

        // Vérification si le dernier gap est le plus grand
        if (currentGapSize > maxGapSize) bestAngle = currentGapStart + currentGapSize / 2;

        // Correction pour éviter des valeurs en dehors de [0,180]
        bestAngle = Math.Clamp(bestAngle, 0, 180);

        return bestAngle;
    }

    
    public void Run()
    {
        IsRunning = true;
        var lidar = new RPLidar("/dev/ttyUSB0", 256000);
        Steering.SetDirection(0);
        Thread.Sleep(4000);
        lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;

        Thrust.SetSpeed(speed);

        var bestAngle = 90;
        while (IsRunning)
        {
            bestAngle = FindBestDirection();
            AdjustSpeed(bestAngle); // Ajuster la vitesse en fonction de la direction
            Steering.SetDirection(Misc.ExponentialMap(bestAngle, 0, 180, -100, 100));
        }

        Thrust.Dispose();
        Steering.Dispose();
        lidar.Dispose();
    }

    /// <summary>
    /// Fonction permettant d'ajuster la vitesse en fonction des distances mesurées 
    /// </summary>
    /// <param name="bestAngle"></param>
    private void AdjustSpeed(int bestAngle)
    {
        var angleRange = 30; // Plage de ±30 degrés autour de la direction
        var minDistanceInRange = float.MaxValue;

        // Trouver la distance minimale dans la plage d'angles
        for (var i = bestAngle - angleRange; i <= bestAngle + angleRange; i++)
        {
            var clampedIndex = Math.Clamp(i, 0, 179);
            if (LidarPoints[clampedIndex] > 0 && LidarPoints[clampedIndex] < minDistanceInRange)
                minDistanceInRange = LidarPoints[clampedIndex];
        }

      
        if (minDistanceInRange == float.MaxValue) minDistanceInRange = Radius * 2; // Valeur par défaut

      
        var speedFactor = Math.Clamp((minDistanceInRange - Radius) / Radius, 0, 1);
        speed = (int)(minSpeed + (maxSpeed - minSpeed) * speedFactor);

        
        Thrust.SetSpeed(speed);

        //  Console.WriteLine($"Min Distance in Range: {minDistanceInRange}, Adjusted Speed: {speed}");
    }

    private void Lidar_LidarPointScanEvent(List<LidarPoint> points)
    {
        var currentTime = DateTime.Now;

        //ne met à jour que les angles reçus
        foreach (var point in points)
        {
            var angleIndex = (int)Math.Round(point.Angle) % 360;
            LidarMemory360[angleIndex] = point.Distance;
            LidarTimestamps360[angleIndex] = currentTime; // Mettre à jour le timestamp
        }

        // Ignore les points trop anciens
        for (var i = 0; i < 360; i++)
            if ((currentTime - LidarTimestamps360[i]).TotalSeconds > 0.6f)
                LidarMemory360[i] = 0; // Ignorer les points trop vieux

        // Transformer les 360° en 180° en replaçant les valeurs correctement
        for (var i = 0; i < 180; i++)
        {
            var lidarAngle = (i - 90 + 360) % 360; // Décalage pour centrer sur 90°
            LidarPoints[i] = LidarMemory360[lidarAngle];
        }

        // Interpolation des gaps
        InterpolateGaps();
    }

    /// <summary>
    /// Interpolation des données pour combler les espaces vide dans les données du lidar
    /// </summary>
    private void InterpolateGaps()
    {
        for (var i = 1; i < 180; i++)
            if (LidarPoints[i] == 0 && LidarPoints[i - 1] != 0)
            {
                // Trouver la fin du gap
                var gapEnd = i;
                while (gapEnd < 180 && LidarPoints[gapEnd] == 0) gapEnd++;

                // Interpoler entre le début et la fin du gap
                if (gapEnd < 180)
                {
                    var startValue = LidarPoints[i - 1];
                    var endValue = LidarPoints[gapEnd];
                    for (var j = i; j < gapEnd; j++)
                    {
                        var t = (float)(j - i + 1) / (gapEnd - i + 1);
                        LidarPoints[j] = startValue + t * (endValue - startValue);
                    }
                }
            }
    }

    public void Stop()
    {
        IsRunning = false;
    }
}