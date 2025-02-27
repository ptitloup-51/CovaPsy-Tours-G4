namespace VoitureAutonome;

public class Lidar
{
    private RPLidar lidar;
    private Dictionary<string, object> Info;
    private (string, int) Health;
    
    // Création d'un dictionnaire avec des clés de 0 à 360
    Dictionary<int, (bool valid, int quality, double exactAngle, double distance)> angleMeasures = new();
    
    // Liste pour stocker toutes les mesures valides
    List<(bool valid, int quality, double angle, double distance)> validMeasures = new();
    
    public Lidar(string port = "/dev/ttyUSB0", int baudRate = 256000)
    {
        lidar = new RPLidar(port, baudRate);
        
        lidar.Connect();
        lidar.StopMotor();
        
        Info = lidar.GetInfo();
        if (Info == null) Console.WriteLine("Impossible d'obtenir les informations du LIDAR");

        Health = lidar.GetHealth();
        lidar.CleanInput();
    }
    
    
    public void Mesure()
    {
        lidar.CleanInput();
        lidar.Start();
        
        var measureCount = 0;
        var maxMeasures = 1000; // Augmentation du nombre de mesures pour une meilleure couverture

        try
        {
             foreach (var mes in lidar.IterMeasures())
        {
            measureCount++;
            if (measureCount > maxMeasures) break;

            double angle = mes.Item3;

            // Normaliser l'angle entre 0 et 360
            while (angle < 0) angle += 360;
            while (angle >= 360) angle -= 360;

            // Ne conserver que les mesures de qualité non nulle et distance non nulle
            if (mes.Item2 > 0 && mes.Item4 > 0)
            {
                // Trouver l'angle entier le plus proche
                var closestIntAngle = (int)Math.Round(angle);
                if (closestIntAngle == 360) closestIntAngle = 0;

                // Si cette mesure est de meilleure qualité que celle existante
                if (!angleMeasures.ContainsKey(closestIntAngle) ||
                    angleMeasures[closestIntAngle].quality < mes.Item2)
                    angleMeasures[closestIntAngle] = (mes.Item1, mes.Item2, angle, mes.Item4);

                // Ajouter à la liste des mesures valides
                validMeasures.Add((mes.Item1, mes.Item2, angle, mes.Item4));
            }

            // Afficher quelques mesures pour déboguer
       /*     if (angle >= 0 && angle <= 10)
                Console.WriteLine(
                    $"Raw: Measure: {mes.Item1}, Quality: {mes.Item2}, Angle: {angle}, Distance: {mes.Item4}"); */
        }
        
        // Arrêter le scan après avoir collecté les données
        lidar.Stop();
        
        // Remplir les angles manquants avec les mesures les plus proches
        for (var i = 0; i <= 360; i++)
            if (!angleMeasures.ContainsKey(i) || angleMeasures[i].quality == 0)
            {
                // Trouver la mesure valide la plus proche pour cet angle
                var closestMeasure = validMeasures
                    .OrderBy(m => Math.Abs(m.angle - i))
                    .FirstOrDefault(m => m.quality > 0 && m.distance > 0);

                if (closestMeasure.quality > 0)
                {
                    angleMeasures[i] = (closestMeasure.valid, closestMeasure.quality,
                        closestMeasure.angle, closestMeasure.distance);
                }
                else
                {
                    // Si aucune mesure valide proche n'est trouvée, utiliser une mesure par défaut
                    // On cherche la moyenne des mesures valides les plus proches
                    var nearestMeasures = validMeasures
                        .OrderBy(m => Math.Abs(m.angle - i))
                        .Take(3)
                        .ToList();

                    if (nearestMeasures.Count > 0)
                    {
                        var avgDistance = nearestMeasures.Average(m => m.distance);
                        double avgAngle = i; // On conserve l'angle entier
                        var avgQuality = (int)nearestMeasures.Average(m => m.quality);

                        angleMeasures[i] = (true, avgQuality, avgAngle, avgDistance);
                    }
                    else
                    {
                        // Cas extrême: aucune mesure valide n'est disponible
                        // On met une valeur par défaut raisonnable (à ajuster selon vos besoins)
                        angleMeasures[i] = (false, 1, i, 500.0);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            lidar.StopMotor();
            lidar.Disconnect();
        }
        
    }

    public void PrintResult()
    {
        // Affichage des distances pour chaque angle
        Console.WriteLine("\nDistances par angle (0-360):");
        for (var i = 0; i <= 360; i++)
        {
            var data = angleMeasures[i];
            Console.WriteLine(
                $"Angle: {i}° -> Distance: {data.distance:F2}, Angle exact: {data.exactAngle:F2}°, Qualité: {data.quality}");
        }
    }

    public void PictureResult(string filename = "/home/covapsytours5/Documents/Lidar.png")
    {
        // Générer l'image
        LidarMapGenerator mapGenerator = new LidarMapGenerator(imageSize: 1000, scale: 0.1f);
        var map = mapGenerator.GenerateMap(angleMeasures);

        // Sauvegarder l'image
        string filePath = "/home/covapsytours5/Documents/map.png";
        mapGenerator.SaveMap(map, filePath);
        Console.WriteLine($"Carte sauvegardée à {filePath}");
    }
}