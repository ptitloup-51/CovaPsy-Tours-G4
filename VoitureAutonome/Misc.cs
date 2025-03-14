using System.Text;

namespace VoitureAutonome;


public class Misc
{
    private static float Q = 0.01f;  // Bruit du modèle
    private static float R = 0.1f;   // Bruit de mesure du LiDAR
    
    
    /// <summary>
    /// Permet de maper des valeurs de façon linéaire
    /// </summary>
    /// <param name="angle"> valeur à mapper</param>
    /// <param name="minAngle"> valeur minimal que l'entrée peut avoir</param>
    /// <param name="maxAngle"> valeur maximale que l'entrée peut avoir</param>
    /// <param name="minOutput"> valeur minimal en sortie</param>
    /// <param name="maxOutput"> valeur maximal en sortie</param>
    /// <returns></returns>
    public int LinearMap(float angle, float minAngle, float maxAngle, float minOutput, float maxOutput)
    {
        // Mapper linéairement l'angle entre minOutput et maxOutput
        float output = minOutput + (angle - minAngle) * (maxOutput - minOutput) / (maxAngle - minAngle);
        return (int)output;
    }
    
    /// <summary>
    /// Permet de mapper une valeur avec une exponentielle sur les valeurs extremes
    /// </summary>
    /// <param name="angle"> valeur à mapper</param>
    /// <param name="minAngle"> valeur minimum que peut prendre l'entrée</param>
    /// <param name="maxAngle"> valeur maximale que peut prendre l'entrée</param>
    /// <param name="minOutput"> valeur minimale de la sortie</param>
    /// <param name="maxOutput"> valeur maximale de la sortie</param>
    /// <returns></returns>
    public int ExponentialMap(float angle, float minAngle, float maxAngle, float minOutput, float maxOutput)
    {
        // Normaliser l'angle entre -1 et 1
        float normalizedAngle = (angle - minAngle) / (maxAngle - minAngle) * 2 - 1;

        // Appliquer une fonction exponentielle pour amplifier les valeurs extrêmes
        float exponent = 0.5f; // Ajustez ce facteur pour contrôler l'agressivité du braquage //0.05f
        float expValue = (float)Math.Pow(Math.Abs(normalizedAngle), exponent);

        // Restaurer le signe
        expValue *= Math.Sign(normalizedAngle);

        // Remapper entre minOutput et maxOutput
        float output = (expValue + 1) / 2 * (maxOutput - minOutput) + minOutput;

        return (int)output;
    }
    
    /// <summary>
    /// Algorithme de filtrage des données, retourne un tableau filtré par l'algorithme de Kalman
    /// </summary>
    /// <param name="input"> tableau d'entrée à filtrer</param>
    /// <returns> retourne un tableau filtré via l'algorithme de Kalman</returns>
    public static float[] KalmanFilter(float[] input)
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

    /// <summary>
    /// Permet d'enregistrer dans un CSV un tableau à deux colonnes
    /// </summary>
    /// <param name="input"> tableau 1</param>
    /// <param name="filteredInput"> tableau 2</param>
    /// <param name="path"> chemin ou le fichier CSV sera enregistré</param>
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

    /// <summary>
    /// Permet de tester le filtrage de Kalman
    /// </summary>
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