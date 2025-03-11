using System.Text;

namespace VoitureAutonome;


public class Misc
{
    private static float Q = 0.01f;  // Bruit du modèle
    private static float R = 0.1f;   // Bruit de mesure du LiDAR
    
    
    public int MapValue(int x, int inMin, int inMax, int outMin, int outMax)
    {
        return outMin + (x - inMin) * (outMax - outMin) / (inMax - inMin);
    }
    
    public float[] KalmanFilter(float[] input)
    {
        
        
        int n = input.Length;
        float[] dEst = new float[n]; // Distances estimées
        float[] P = new float[n];    // Incertitudes
        float[] filtered = new float[n];

        // Initialisation : on suppose que la première mesure est correcte
        dEst[0] = input[0]; // Initialisation avec la première valeur seulement
        P[0] = 1f;

        for (int i = 1; i < n; i++)  // Débuter à i = 1 (pas 0)
        {
            // Prédiction basée sur la valeur filtrée précédente
            float dPred = dEst[i - 1];  
            float PPred = P[i - 1] + Q;

            // Gain de Kalman
            float K = PPred / (PPred + R);

            // Mise à jour avec la mesure actuelle
            dEst[i] = dPred + K * (input[i] - dPred);
            P[i] = (1 - K) * PPred;

            // Stocker la valeur filtrée
            filtered[i] = dEst[i];
        }
        
        return filtered;
    }

    public void ArrayToCSV(float[] input, float[] filteredInput, string path = "/home/covapsytours5/Documents/result.csv")
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Angle,Mesure Brute,Mesure Filtrée");

        for (int i = 0; i < input.Length; i++)
        {
            csv.AppendLine($"{i},{input[i]:F3},{filteredInput[i]:F3}");
        }

        File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
        Console.WriteLine($"Fichier CSV généré : {path}");
    }

    public void Test()
    {
        Random rand = new Random();
        float[] lidarRaw = new float[180];

        // Générer des valeurs bruitées (simulant un LiDAR)
        for (int i = 0; i < 180; i++)
        {
            lidarRaw[i] = (float)(rand.NextDouble() * 10); // Valeurs entre 0 et 10m
        }

        // Appliquer le filtre de Kalman
        float[] lidarFiltered = KalmanFilter(lidarRaw);

       
        ArrayToCSV(lidarRaw, lidarFiltered);
    }
    
    
    
}