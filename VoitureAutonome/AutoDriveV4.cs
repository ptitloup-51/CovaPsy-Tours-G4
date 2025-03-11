using System;
using System.Collections.Generic;
using System.Threading;
using RpLidar.NET;
using RpLidar.NET.Entities;

namespace VoitureAutonome
{
    public class AutoDriveV4
    {
        public bool isRunning { get; set; }
        private float[] LidarPoints = new float[180];

        Misc Misc = new Misc();
        Steering Steering = new Steering();
        Thrust Thrust = new Thrust();

        public float Radius = 400.0f; // Rayon de sécurité autour de l'obstacle

        public AutoDriveV4()
        {
            isRunning = false;
        }

        public void Run()
        {
            isRunning = true;

            var lidar = new RPLidar("/dev/ttyUSB0", 256000);
            Steering.SetDirection(0);
            Thread.Sleep(5000); // Attendre que le LIDAR soit prêt
            lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
            Thrust.SetSpeed(15);

            while (isRunning)
            {
                int bestAngle = (int)FTG();
                int steeringValue = Misc.MapValue(bestAngle, 0, 180, -100, 100);
                Steering.SetDirection(steeringValue);
              //  Console.WriteLine($"Meilleur angle : {bestAngle}, Direction : {steeringValue}");
              //  Thread.Sleep(100); // Attendre avant la prochaine itération
            }

            Thrust.Dispose();
            Steering.Dispose();
            lidar.Dispose();
        }

        public float FTG()
        {
            float minDistance = 10000;
            int angle = 0;

            // Trouver la distance minimale valide et l'angle correspondant
            for (int i = 0; i < 180; i++)
            {
                if (LidarPoints[i] > 0 && LidarPoints[i] < minDistance) // Ignorer les distances nulles
                {
                    minDistance = LidarPoints[i];
                    angle = i;
                }
            }

            // Si aucune distance valide n'est trouvée, rester à l'arrêt
            if (minDistance == 10000)
            {
                Console.WriteLine("Aucune mesure valide. Arrêt.");
                return 90; // Angle neutre (tout droit)
            }

            // Calcul des angles compris dans le cercle de sécurité
            float alpha = (float)Math.Acos(minDistance / Math.Sqrt(Math.Pow(minDistance, 2) + Math.Pow(Radius, 2))) *
                          (180 / (float)Math.PI);

            int startangle = Math.Clamp(angle - (int)Math.Round(alpha), 0, 179);
            int endangle = Math.Clamp(angle + (int)Math.Round(alpha), 0, 179);

            // Trouver le plus grand espace libre (gap)
            int maxGapSize = 0;
            int bestAngle = 90; // Angle par défaut (tout droit)
            int currentGapStart = 0;

            for (int i = 0; i < 180; i++)
            {
                if (i < startangle || i > endangle)
                {
                    if (LidarPoints[i] > minDistance + Radius && LidarPoints[i] > 0) // Ignorer les distances nulles
                    {
                        if (currentGapStart == 0)
                        {
                            currentGapStart = i;
                        }

                        int gapSize = i - currentGapStart;
                        if (gapSize > maxGapSize)
                        {
                            maxGapSize = gapSize;
                            bestAngle = currentGapStart + gapSize / 2; // Angle médian du gap
                        }
                    }
                    else
                    {
                        currentGapStart = 0;
                    }
                }
            }

            // Retourner l'angle médian du plus grand gap (0 = gauche, 180 = droite)
            return bestAngle;
        }

        public void Stop()
        {
            isRunning = false;
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
                if (point.Quality > 10 && point.Distance > 0) // Ignorer les mesures de qualité insuffisante ou nulles
                {
                    int angleIndex = (int)Math.Round(point.Angle) % 360;
                    lidarMemory360[angleIndex] = point.Distance; // Mettre à jour la mémoire LIDAR
                }
            }

            // Transformer les 360° en 180° en replaçant les valeurs correctement
            for (int i = 0; i < 180; i++)
            {
                int lidarAngle = (i - 90 + 360) % 360; // Décalage pour centrer sur 90°
                rawData180[i] = lidarMemory360[lidarAngle];
            }

            // Appliquer le filtre de Kalman pour lisser les données
            LidarPoints = Misc.KalmanFilter(rawData180);

            LastTime = DateTime.Now;
        }
    }
}