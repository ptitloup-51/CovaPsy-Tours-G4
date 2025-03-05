using VoitureAutonome;
using System;
using System.Diagnostics;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class Program
{
    static void Main(string[] args)
    {
        
        
        
        
        
        
        Thrust thrust = new Thrust();
        Steering steering = new Steering();
       

      //  var lidarMapGenerator = new LidarMapGenerator(imageSize: 1000, scale: 1.0f); // Création du générateur de carte LIDAR
      
        var lidarScanner = new RPLidarScanner("/dev/ttyUSB0");
        lidarScanner.StartScanning();
        Thread.Sleep(3000);
        thrust.SetSpeed(2);

        while (true)
        {
            try
            {
                var scanData = lidarScanner.GetLatestScan();
                // 2. Filtrer et lisser les données du scan (moyenne des angles voisins)
                var smoothedData = SmoothScanData(scanData);
            
                // 3. Trouver l'angle avec la plus grande distance
                var maxEntry = smoothedData.Aggregate((l, r) => l.Value > r.Value ? l : r);
                int maxAngle = maxEntry.Key;

                int error = 90 - maxAngle;
              //  Console.WriteLine(error); // Affichage de l'erreur, entre le centre (90°) et l'angle le plus long
                //positif = gauce, négatif = droite
                
                steering.SetDirection(MapValue(error, -90, 90, 18, -18));
              //  Console.WriteLine($" error : {error}, steering : {MapValue(error, -90, 90, -18, 18)}");

               // Thread.Sleep(500);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
               
            }
            
            
            
            
        }
       

       
        


    }
    
    static int MapValue(int x, int inMin, int inMax, int outMin, int outMax)
    {
        return outMin + (x - inMin) * (outMax - outMin) / (inMax - inMin);
    }
    
    // Fonction pour lisser les données du LIDAR
    private static Dictionary<int, float> SmoothScanData(Dictionary<int, float> scanData)
    {
        var smoothedData = new Dictionary<int, float>();

        foreach (var angle in scanData.Keys)
        {
            // Moyenne des distances des 3 à 4 angles voisins (par exemple, 2 à gauche et 2 à droite)
            float smoothedDistance = GetAverageOfNeighboringAngles(angle, scanData);
            smoothedData[angle] = smoothedDistance;
        }

        return smoothedData;
    }

// Fonction pour calculer la moyenne des distances des 3 à 4 angles voisins autour d'un angle donné
    private static float GetAverageOfNeighboringAngles(int angle, Dictionary<int, float> scanData)
    {
        // Nombre d'angles à prendre en compte pour la moyenne (ici on prend 2 à gauche et 2 à droite)
        int range = 2;

        float totalDistance = 0;
        int count = 0;

        for (int offset = -range; offset <= range; offset++)
        {
            int neighborAngle = (angle + offset + 360) % 360; // S'assurer que l'angle est dans la plage 0-359
            if (scanData.ContainsKey(neighborAngle))
            {
                totalDistance += scanData[neighborAngle];
                count++;
            }
        }

        return totalDistance / count; // Retourne la moyenne
    }


    
    
}