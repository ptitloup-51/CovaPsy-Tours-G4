using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace VoitureAutonome;

public class ContinuousLidar : IDisposable
{
    private RPLidar lidar;
    private Dictionary<string, object> Info;
    private (string, int) Health;
    
    private ConcurrentDictionary<int, (bool valid, int quality, double exactAngle, double distance)> angleMeasures = new();
    private ConcurrentBag<(bool valid, int quality, double angle, double distance)> validMeasures = new();
    
    private bool isRunning = false;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    
    private int maxMeasuresPerScan = 180; // Réduit pour améliorer la vitesse
    private readonly string lidarPort;
    private readonly int lidarBaudRate;
    private readonly object lockObject = new object();
    private readonly string imageDirectory;
    private int imageCounter = 0;
    
    // Indicateur pour éviter la réinitialisation en boucle
    private bool isReconnecting = false;
    
    public ContinuousLidar(string port = "/dev/ttyUSB0", int baudRate = 256000, string imageDir = "/home/covapsytours5/Documents/images/")
    {
        lidarPort = port;
        lidarBaudRate = baudRate;
        imageDirectory = imageDir;
        
        // S'assurer que le répertoire d'images existe
        Directory.CreateDirectory(imageDirectory);
        
        // Initialiser le LIDAR
        InitializeLidar();
    }
    
    private void InitializeLidar()
    {
        try
        {
            lidar = new RPLidar(lidarPort, lidarBaudRate);
            
            lidar.Connect();
            lidar.StopMotor();
            
            // Essayons de nettoyer et réinitialiser le LIDAR avant de commencer
            lidar.CleanInput();
            lidar.Reset();  // Ajout d'une réinitialisation complète
            Thread.Sleep(1000);  // Attendre après la réinitialisation
            
            Info = lidar.GetInfo();
            if (Info == null) 
                Console.WriteLine("Impossible d'obtenir les informations du LIDAR");

            Health = lidar.GetHealth();
            lidar.CleanInput();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur d'initialisation du LIDAR: {ex.Message}");
            throw;
        }
    }
    
    public void StartContinuousMapping(int scanIntervalMs = 100, int imageIntervalMs = 500)
    {
        if (isRunning)
            return;
            
        isRunning = true;
        
        // Démarrer le thread de mesure continue
        Task.Run(() => ContinuousMeasurementLoop(scanIntervalMs), cancellationTokenSource.Token);
        
        // Démarrer le thread de génération d'images
        Task.Run(() => ImageGenerationLoop(imageIntervalMs), cancellationTokenSource.Token);
        
        Console.WriteLine("Cartographie continue démarrée");
    }
    
    public void StopContinuousMapping()
    {
        if (!isRunning)
            return;
            
        isRunning = false;
        cancellationTokenSource.Cancel();
        
        // Attendre un peu pour que les threads s'arrêtent proprement
        Thread.Sleep(500);
        
        // Arrêter le LIDAR
        try
        {
            if (lidar != null)
            {
                try { lidar.Stop(); } catch { }
                try { lidar.StopMotor(); } catch { }
                try { lidar.Disconnect(); } catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'arrêt du LIDAR: {ex.Message}");
        }
        
        Console.WriteLine("Cartographie continue arrêtée");
    }
    
    private async Task ContinuousMeasurementLoop(int intervalMs)
    {
        int errorCount = 0;
        
        while (!cancellationTokenSource.Token.IsCancellationRequested && isRunning)
        {
            try
            {
                Stopwatch sw = new();
                sw.Start();
                
                // Obtenir une nouvelle mesure
                PerformQuickMeasurement();
                
                // Réinitialiser le compteur d'erreurs après une mesure réussie
                errorCount = 0;
                
                sw.Stop();
                
                // Afficher le temps de mesure pour le débogage
                Console.WriteLine($"Temps de mesure: {sw.ElapsedMilliseconds}ms");
                
                // Attendre l'intervalle spécifié moins le temps déjà écoulé
                int delayTime = Math.Max(0, intervalMs - (int)sw.ElapsedMilliseconds);
                if (delayTime > 0 && isRunning)
                    await Task.Delay(delayTime, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal lors de l'annulation
                break;
            }
            catch (Exception ex)
            {
                errorCount++;
                Console.WriteLine($"Erreur dans la boucle de mesure: {ex.Message}");
                
                // Si trop d'erreurs consécutives, essayer de réinitialiser le LIDAR
                if (errorCount > 3 && !isReconnecting && isRunning)
                {
                    isReconnecting = true;
                    Console.WriteLine("Tentative de réinitialisation du LIDAR après plusieurs échecs...");
                    
                    try
                    {
                        lock (lockObject)
                        {
                            try { lidar.Stop(); } catch { }
                            try { lidar.StopMotor(); } catch { }
                            try { lidar.Disconnect(); } catch { }
                            
                            Thread.Sleep(2000);
                            InitializeLidar();
                            errorCount = 0;
                        }
                    }
                    catch (Exception resetEx)
                    {
                        Console.WriteLine($"Échec de la réinitialisation du LIDAR: {resetEx.Message}");
                    }
                    finally
                    {
                        isReconnecting = false;
                    }
                }
                
                // Attendre avant de réessayer
                if (isRunning)
                    await Task.Delay(1000, cancellationTokenSource.Token);
            }
        }
    }
    
    private async Task ImageGenerationLoop(int intervalMs)
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested && isRunning)
        {
            try
            {
                Stopwatch sw = new();
                sw.Start();
                
                // Vérifier si nous avons des données suffisantes pour générer une image
                if (angleMeasures.Count > 0)
                {
                    // Générer l'image
                    string filename = $"{imageDirectory}lidar_map_{imageCounter++}.png";
                    GenerateImage(filename);
                    
                    sw.Stop();
                    
                    // Afficher le temps de génération d'image pour le débogage
                    Console.WriteLine($"Temps de génération d'image: {sw.ElapsedMilliseconds}ms");
                }
                else
                {
                    Console.WriteLine("Pas assez de données pour générer une image");
                }
                
                // Attendre l'intervalle spécifié moins le temps déjà écoulé
                int delayTime = intervalMs;
                if (sw.IsRunning)
                {
                    sw.Stop();
                    delayTime = Math.Max(0, intervalMs - (int)sw.ElapsedMilliseconds);
                }
                
                if (delayTime > 0 && isRunning)
                    await Task.Delay(delayTime, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal lors de l'annulation
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans la boucle de génération d'images: {ex.Message}");
                if (isRunning)
                    await Task.Delay(1000, cancellationTokenSource.Token);
            }
        }
    }
    
    private void PerformQuickMeasurement()
    {
        lock (lockObject)
        {
            if (!isRunning)
                return;
                
            // Nettoyer les mesures précédentes pour cette itération
            var newValidMeasures = new ConcurrentBag<(bool valid, int quality, double angle, double distance)>();
            
            try
            {
                // Vérifier que le LIDAR est connecté
                if (lidar == null)
                {
                    InitializeLidar();
                    Thread.Sleep(500);
                }
                
                lidar.CleanInput();
                
                // Utiliser "normal" car le mode express semble causer des problèmes
                string mode = "normal";
                
                // Démarrer le moteur avant le scan
                lidar.StartMotor();
                Thread.Sleep(200);  // Laisser le moteur atteindre sa vitesse
                
                lidar.Start(mode);
                
                var measureCount = 0;
                
                var mesure = lidar.IterMeasures(mode);
                
                // Collecter un nombre limité de mesures pour assurer la rapidité
                foreach (var mes in mesure)
                {
                    measureCount++;
                    if (measureCount > maxMeasuresPerScan) break;

                    double angle = mes.Item3;

                    // Normaliser l'angle entre 0 et 360
                    while (angle < 0) angle += 360;
                    while (angle >= 360) angle -= 360;

                    // Ne conserver que les mesures avec distance non nulle
                    if (mes.Item4 > 0)
                    {
                        // Trouver l'angle entier le plus proche
                        var closestIntAngle = (int)Math.Round(angle);
                        if (closestIntAngle == 360) closestIntAngle = 0;

                        // Mettre à jour les mesures d'angle de manière thread-safe
                        angleMeasures[closestIntAngle] = (mes.Item1, mes.Item2, angle, mes.Item4);

                        // Ajouter à la liste des mesures valides
                        newValidMeasures.Add((mes.Item1, mes.Item2, angle, mes.Item4));
                    }
                }
                
                // Mettre à jour les mesures valides
                validMeasures = newValidMeasures;
                
                // Remplir les angles manquants pour compléter la carte
                FillMissingAngles();
                
                // Arrêter le scan après avoir collecté les données
                lidar.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erreur lors de la mesure: {e.Message}");
                throw;
            }
            finally
            {
                try
                {
                    // Ne pas appeler SetPwm directement, car cela ne semble pas fonctionner
                    // Laisser le moteur tourner à vitesse normale
                    if (isRunning)
                    {
                        // Conserver le moteur en marche pour les mesures suivantes
                        // mais sans appeler SetPwm qui semble problématique
                    }
                    else
                    {
                        // Si le programme est en train de s'arrêter, arrêter le moteur
                        lidar.StopMotor();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'ajustement du moteur: {ex.Message}");
                }
            }
        }
    }
    
    private void FillMissingAngles()
    {
        // Version simplifiée pour interpoler les angles manquants
        var angleKeys = angleMeasures.Keys.ToList();
        if (angleKeys.Count < 2)
            return;  // Pas assez de données pour l'interpolation
            
        angleKeys.Sort();
        
        // Pour chaque angle manquant entre 0 et 359
        for (int angle = 0; angle < 360; angle++)
        {
            if (angleMeasures.ContainsKey(angle))
                continue;  // Angle déjà mesuré
                
            // Trouver l'angle inférieur le plus proche
            int lowerAngle = -1;
            for (int i = angleKeys.Count - 1; i >= 0; i--)
            {
                if (angleKeys[i] < angle || (angle == 0 && angleKeys[i] > 270))  // Gestion spéciale pour l'angle 0
                {
                    lowerAngle = angleKeys[i];
                    break;
                }
            }
            
            // Trouver l'angle supérieur le plus proche
            int upperAngle = -1;
            for (int i = 0; i < angleKeys.Count; i++)
            {
                if (angleKeys[i] > angle || (angle > 270 && angleKeys[i] < 90))  // Gestion spéciale pour les angles près de 360
                {
                    upperAngle = angleKeys[i];
                    break;
                }
            }
            
            // Si nous n'avons pas trouvé d'angle inférieur, utiliser le dernier
            if (lowerAngle == -1 && angleKeys.Count > 0)
                lowerAngle = angleKeys[angleKeys.Count - 1];
                
            // Si nous n'avons pas trouvé d'angle supérieur, utiliser le premier
            if (upperAngle == -1 && angleKeys.Count > 0)
                upperAngle = angleKeys[0];
                
            // Si nous avons des angles pour interpoler
            if (lowerAngle != -1 && upperAngle != -1)
            {
                // Obtenir les valeurs à ces angles
                if (angleMeasures.TryGetValue(lowerAngle, out var lowerValue) && 
                    angleMeasures.TryGetValue(upperAngle, out var upperValue))
                {
                    // Calculer la distance angulaire (en tenant compte du bouclage à 360°)
                    int angularDistance;
                    if (upperAngle > lowerAngle)
                        angularDistance = upperAngle - lowerAngle;
                    else
                        angularDistance = (upperAngle + 360) - lowerAngle;
                        
                    // Calculer la distance de l'angle actuel par rapport à l'angle inférieur
                    int distanceFromLower;
                    if (angle >= lowerAngle)
                        distanceFromLower = angle - lowerAngle;
                    else
                        distanceFromLower = (angle + 360) - lowerAngle;
                        
                    // Calculer le ratio pour l'interpolation
                    double ratio = (double)distanceFromLower / angularDistance;
                    
                    // Interpolation linéaire
                    double distance = lowerValue.distance + ratio * (upperValue.distance - lowerValue.distance);
                    
                    // Interpolation de la qualité
                    int quality = (int)(lowerValue.quality + ratio * (upperValue.quality - lowerValue.quality));
                    
                    // Ajouter la mesure interpolée
                    angleMeasures[angle] = (true, quality, angle, distance);
                }
                else
                {
                    // Valeur par défaut si on ne peut pas récupérer les valeurs
                    angleMeasures[angle] = (false, 1, angle, 500.0);
                }
            }
            else
            {
                // Valeur par défaut si on ne peut pas faire l'interpolation
                angleMeasures[angle] = (false, 1, angle, 500.0);
            }
        }
    }
    
    private void GenerateImage(string filename)
    {
        try
        {
            // Créer une copie des données actuelles pour éviter des modifications pendant la génération
            var currentData = new Dictionary<int, (bool valid, int quality, double exactAngle, double distance)>();
            foreach (var pair in angleMeasures)
            {
                currentData[pair.Key] = pair.Value;
            }
            
            // Générer l'image
            LidarMapGenerator mapGenerator = new LidarMapGenerator(imageSize: 1000, scale: 0.1f);
            var map = mapGenerator.GenerateMap(currentData);

            // Sauvegarder l'image
            mapGenerator.SaveMap(map, filename);
            Console.WriteLine($"Carte sauvegardée à {filename} avec {mapGenerator.Points} Points");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la génération de l'image: {ex.Message}");
        }
    }
    
    // Propriété pour accéder aux mesures actuelles
    public IReadOnlyDictionary<int, (bool valid, int quality, double exactAngle, double distance)> CurrentMeasures
    {
        get
        {
            var snapshot = new Dictionary<int, (bool valid, int quality, double exactAngle, double distance)>();
            foreach (var pair in angleMeasures)
            {
                snapshot[pair.Key] = pair.Value;
            }
            return snapshot;
        }
    }
    
    // Implémentation de IDisposable pour nettoyer les ressources
    public void Dispose()
    {
        StopContinuousMapping();
    }
}