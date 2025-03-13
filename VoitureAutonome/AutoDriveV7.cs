using System;
using System.Collections.Generic;
using System.Threading;
using RpLidar.NET;
using RpLidar.NET.Entities;

namespace VoitureAutonome
{
    public class AutoDriveV7
    {
        public bool isRunning { get; set; }
        private float[] LidarPoints = new float[180]; // Tableau pour les 180° avant
        private DateTime[] LidarTimestamps = new DateTime[180]; // Timestamps des points LIDAR

        Misc Misc = new Misc();
        Steering Steering = new Steering();
        Thrust Thrust = new Thrust();

        public float Radius = 500.0f; // Rayon de sécurité accru
        public int BaseSpeed = 20; // Vitesse de base
        public int TurnSpeed = 10; // Vitesse réduite dans les virages
        public float EmergencyStopDistance = 200.0f; // Distance d'arrêt d'urgence
        public float MaxPointAgeSeconds = 0.5f; // Durée de vie maximale d'un point LIDAR (en secondes)

        public AutoDriveV7()
        {
            isRunning = false;
        }

        public void Run()
        {
            isRunning = true;

            var lidar = new RPLidar("/dev/ttyUSB0", 256000);
            Steering.SetDirection(0);
            // Thread.Sleep(5000); // Attendre que le LIDAR soit prêt
            lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
            Thrust.SetSpeed(BaseSpeed);

            while (isRunning)
            {
                int bestAngle = (int)FTG();
                int steeringValue = Misc.ExponentialMap(bestAngle, 0, 180, -100, 100);
                Steering.SetDirection(steeringValue);

                // Ajuster la vitesse en fonction de l'angle de virage et de la distance aux obstacles
                int speed = CalculateSafeSpeed(bestAngle);
                Thrust.SetSpeed(speed);

                // Console.WriteLine($"Meilleur angle : {bestAngle}, Direction : {steeringValue}, Vitesse : {speed}");
               // Thread.Sleep(20); // Réduire le délai pour une réactivité accrue
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
                if (IsPointValid(i) && LidarPoints[i] > 0 && LidarPoints[i] < minDistance) // Ignorer les points invalides et les distances nulles
                {
                    minDistance = LidarPoints[i];
                    angle = i;
                }
            }

            /*
            // Si la distance est trop faible, arrêt d'urgence
            if (minDistance < EmergencyStopDistance)
            {
                Console.WriteLine("Obstacle trop proche. Arrêt d'urgence.");
                Thrust.SetSpeed(0);
                return 90; // Angle neutre (tout droit)
            }
            */

            // Ajuster dynamiquement le rayon de sécurité
            Radius = GetDynamicRadius(minDistance);

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
                    if (IsPointValid(i) && LidarPoints[i] > minDistance + Radius && LidarPoints[i] > 0) // Ignorer les points invalides et les distances nulles
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

            /*
            // Vérifier si le gap est suffisamment large
            if (maxGapSize < 20) // Si le gap est trop petit, rester à l'arrêt
            {
                Console.WriteLine("Gap trop petit. Arrêt.");
                return 90; // Angle neutre (tout droit)
            }
            */

            // Retourner l'angle médian du plus grand gap (0 = gauche, 180 = droite)
            return bestAngle;
        }

        public void Stop()
        {
            isRunning = false;
        }

        // Stockage global des valeurs précédentes (mémoire du dernier scan)
        private float[] lidarMemory360 = new float[360];
        private DateTime[] lidarTimestamps360 = new DateTime[360]; // Timestamps des points LIDAR

        private void Lidar_LidarPointScanEvent(List<LidarPoint> points)
        {
            float[] rawData180 = new float[180];

            // Mettre à jour uniquement les angles reçus
            foreach (var point in points)
            {
                int angleIndex = (int)Math.Round(point.Angle) % 360;
                lidarMemory360[angleIndex] = point.Distance; // Mettre à jour la mémoire LIDAR
                lidarTimestamps360[angleIndex] = DateTime.Now; // Mettre à jour le timestamp
            }

            // Transformer les 360° en 180° en replaçant les valeurs correctement
            for (int i = 0; i < 180; i++)
            {
                int lidarAngle = (i - 90 + 360) % 360; // Décalage pour centrer sur 90°
                rawData180[i] = lidarMemory360[lidarAngle];
                LidarTimestamps[i] = lidarTimestamps360[lidarAngle]; // Mettre à jour les timestamps
            }

            // Appliquer le lissage des données
            LidarPoints = SmoothLidarData(rawData180);
        }

        private float[] SmoothLidarData(float[] rawData)
        {
            float[] smoothedData = new float[rawData.Length];
            for (int i = 0; i < rawData.Length; i++)
            {
                if (rawData[i] == 0 && i > 0 && i < rawData.Length - 1)
                {
                    // Interpolation linéaire pour les valeurs manquantes
                    smoothedData[i] = (rawData[i - 1] + rawData[i + 1]) / 2;
                }
                else
                {
                    smoothedData[i] = rawData[i];
                }
            }

            // Filtre passe-bas pour lisser les données
            float alpha = 0.4f; // Facteur de lissage (ajusté pour plus de réactivité)
            for (int i = 1; i < smoothedData.Length; i++)
            {
                smoothedData[i] = alpha * smoothedData[i] + (1 - alpha) * smoothedData[i - 1];
            }

            return smoothedData;
        }

        private bool IsPointValid(int angleIndex)
        {
            // Vérifier si le point est trop ancien
            TimeSpan age = DateTime.Now - LidarTimestamps[angleIndex];
            return age.TotalSeconds <= MaxPointAgeSeconds;
        }

        public float GetDynamicRadius(float minDistance)
        {
            float baseRadius = 900.0f; // Rayon de base accru //800
            float safetyMargin = 230.0f; // Marge de sécurité supplémentaire
            return baseRadius + safetyMargin / minDistance; // Ajuster en fonction de la distance
        }

        private int CalculateSafeSpeed(int bestAngle)
        {
            // Réduire la vitesse si l'angle de virage est important
            if (Math.Abs(bestAngle - 90) > 45) // Si l'angle est supérieur à 45° par rapport à la droite
            {
                return TurnSpeed;
            }

            // Réduire la vitesse si un obstacle est proche
            float minDistance = 10000;
            for (int i = 0; i < 180; i++)
            {
                if (IsPointValid(i) && LidarPoints[i] > 0 && LidarPoints[i] < minDistance)
                {
                    minDistance = LidarPoints[i];
                }
            }

            if (minDistance < EmergencyStopDistance * 2) // Si un obstacle est proche
            {
                return TurnSpeed;
            }

            return BaseSpeed;
        }
    }
}