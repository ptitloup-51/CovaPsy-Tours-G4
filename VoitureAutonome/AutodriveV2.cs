using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RpLidar.NET;
using RpLidar.NET.Entities;

namespace VoitureAutonome
{
    // Version fonctionnelle avec ralentissement dans les virages
    public class AutodriveV2
    {
        private bool IsRunning;
        private float[] LidarPoints = new float[180];
        private float[] LidarMemory360 = new float[360]; // Stockage global des valeurs précédentes
        private DateTime[] LidarTimestamps360 = new DateTime[360]; // Timestamps pour chaque angle
        private Misc Misc = new Misc();
        private Steering Steering = new Steering();
        private Thrust Thrust = new Thrust();

        float Radius = 1400; //Radius
        int maxSpeed = 20; // Vitesse maximale
        int minSpeed = 15; // Vitesse minimale dans les virages
        int speed = 20; // Vitesse actuelle

        public AutodriveV2()
        {
            IsRunning = false;
            Array.Fill(LidarTimestamps360, DateTime.MinValue); // Initialiser les timestamps
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

            for (int i = 0; i < LidarPoints.Length; i++)
            {
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
            }

            // Vérification si le dernier gap est le plus grand
            if (currentGapSize > maxGapSize)
            {
                bestAngle = currentGapStart + currentGapSize / 2;
            }

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

            int bestAngle = 90;
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

        private void AdjustSpeed(int bestAngle)
        {
            int angleRange = 30; // Plage de ±30 degrés autour de la direction
            float minDistanceInRange = float.MaxValue;

            // Trouver la distance minimale dans la plage d'angles
            for (int i = bestAngle - angleRange; i <= bestAngle + angleRange; i++)
            {
                int clampedIndex = Math.Clamp(i, 0, 179);
                if (LidarPoints[clampedIndex] > 0 && LidarPoints[clampedIndex] < minDistanceInRange)
                {
                    minDistanceInRange = LidarPoints[clampedIndex];
                }
            }

            // Si aucune distance valide n'est trouvée, utiliser la distance maximale
            if (minDistanceInRange == float.MaxValue)
            {
                minDistanceInRange = Radius * 2; // Valeur par défaut
            }

            // Ajuster la vitesse en fonction de la distance minimale
            float speedFactor = Math.Clamp((minDistanceInRange - Radius) / Radius, 0, 1);
            speed = (int)(minSpeed + (maxSpeed - minSpeed) * speedFactor);

            // Appliquer la nouvelle vitesse
            Thrust.SetSpeed(speed);

          //  Console.WriteLine($"Min Distance in Range: {minDistanceInRange}, Adjusted Speed: {speed}");
        }

        private void Lidar_LidarPointScanEvent(List<LidarPoint> points)
        {
            DateTime currentTime = DateTime.Now;

            // Mettre à jour uniquement les angles reçus
            foreach (var point in points)
            {
                int angleIndex = (int)Math.Round(point.Angle) % 360;
                LidarMemory360[angleIndex] = point.Distance;
                LidarTimestamps360[angleIndex] = currentTime; // Mettre à jour le timestamp
            }

            // Filtrer les points trop vieux (plus de 0,6 seconde)
            for (int i = 0; i < 360; i++)
            {
                if ((currentTime - LidarTimestamps360[i]).TotalSeconds > 0.6f)
                {
                    LidarMemory360[i] = 0; // Ignorer les points trop vieux
                }
            }

            // Transformer les 360° en 180° en replaçant les valeurs correctement
            for (int i = 0; i < 180; i++)
            {
                int lidarAngle = (i - 90 + 360) % 360; // Décalage pour centrer sur 90°
                LidarPoints[i] = LidarMemory360[lidarAngle];
            }

            // Interpolation des gaps
            InterpolateGaps();
        }

        private void InterpolateGaps()
        {
            for (int i = 1; i < 180; i++)
            {
                if (LidarPoints[i] == 0 && LidarPoints[i - 1] != 0)
                {
                    // Trouver la fin du gap
                    int gapEnd = i;
                    while (gapEnd < 180 && LidarPoints[gapEnd] == 0)
                    {
                        gapEnd++;
                    }

                    // Interpoler entre le début et la fin du gap
                    if (gapEnd < 180)
                    {
                        float startValue = LidarPoints[i - 1];
                        float endValue = LidarPoints[gapEnd];
                        for (int j = i; j < gapEnd; j++)
                        {
                            float t = (float)(j - i + 1) / (gapEnd - i + 1);
                            LidarPoints[j] = startValue + t * (endValue - startValue);
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            IsRunning = false;
        }
    }
}