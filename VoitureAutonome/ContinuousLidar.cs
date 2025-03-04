using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VoitureAutonome
{
    public class ContinuousLidar : IDisposable
    {
        private RPLidar lidar;
        private Dictionary<string, object> Info;
        private (string, int) Health;

        private ConcurrentDictionary<int, (bool valid, int quality, double exactAngle, double distance)> angleMeasures = new();
        private ConcurrentBag<(bool valid, int quality, double angle, double distance)> validMeasures = new();

        private bool isRunning = false;
        private CancellationTokenSource cancellationTokenSource;

        private int maxMeasuresPerScan = 180; // Réduit pour améliorer la vitesse
        private readonly string lidarPort;
        private readonly int lidarBaudRate;
        private readonly object lockObject = new();
        private readonly string imageDirectory;

        private bool isReconnecting = false;

        public ContinuousLidar(string port = "/dev/ttyUSB0", int baudRate = 256000, string imageDir = "/home/covapsytours5/Documents/images/")
        {
            lidarPort = port;
            lidarBaudRate = baudRate;
            imageDirectory = imageDir;

            Directory.CreateDirectory(imageDirectory); // S'assurer que le répertoire d'images existe
            InitializeLidar(); // Initialiser le LIDAR
        }

        private void InitializeLidar()
        {
            try
            {
                lidar = new RPLidar(lidarPort, lidarBaudRate);
                lidar.Connect();
                lidar.StopMotor();

                lidar.CleanInput();
                lidar.Reset(); // Réinitialisation complète
                Thread.Sleep(1000); // Attendre après la réinitialisation

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

        public async Task StartContinuousScanAsync(int scanIntervalMs = 50) // Intervalle réduit pour plus de rapidité
        {
            if (isRunning) return;

            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            await Task.Run(() => ContinuousMeasurementLoop(scanIntervalMs), cancellationTokenSource.Token);
        }

        public void StartContinuousScan(int scanIntervalMs = 50) // Version non-async
        {
            if (isRunning) return;

            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => ContinuousMeasurementLoop(scanIntervalMs), cancellationTokenSource.Token);
            Console.WriteLine("Cartographie continue démarrée");
        }

        public void StopContinuousScan()
        {
            if (!isRunning) return;

            isRunning = false;
            cancellationTokenSource?.Cancel();

            Thread.Sleep(500); // Attendre un peu pour que les threads s'arrêtent proprement

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

        public void TakePhoto(string photoName)
        {
            try
            {
                string filename = $"{imageDirectory}{photoName}.png";
                GenerateImage(filename);
                Console.WriteLine($"Photo sauvegardée: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la génération de l'image: {ex.Message}");
            }
        }

        public double GetDistanceAtAngle(int angle)
        {
            angle = ((angle % 360) + 360) % 360; // Normaliser l'angle entre 0 et 359

            if (angleMeasures.TryGetValue(angle, out var measure))
                return measure.distance;

            return 0; // Pas de données disponibles
        }

        private async Task ContinuousMeasurementLoop(int intervalMs)
        {
            int errorCount = 0;

            while (!cancellationTokenSource.Token.IsCancellationRequested && isRunning)
            {
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    PerformQuickMeasurement(); // Obtenir une nouvelle mesure
                    errorCount = 0; // Réinitialiser le compteur d'erreurs
                    sw.Stop();

                    Console.WriteLine($"Temps de mesure: {sw.ElapsedMilliseconds}ms");

                    int delayTime = Math.Max(0, intervalMs - (int)sw.ElapsedMilliseconds);
                    if (delayTime > 0 && isRunning)
                        await Task.Delay(delayTime, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break; // Normal lors de l'annulation
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"Erreur dans la boucle de mesure: {ex.Message}");

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

                    if (isRunning)
                        await Task.Delay(1000, cancellationTokenSource.Token);
                }
            }
        }

        private void PerformQuickMeasurement()
        {
            lock (lockObject)
            {
                if (!isRunning) return;

                var newValidMeasures = new ConcurrentBag<(bool valid, int quality, double angle, double distance)>();

                try
                {
                    if (lidar == null)
                    {
                        InitializeLidar();
                        Thread.Sleep(500);
                    }

                    lidar.CleanInput();
                    lidar.StartMotor();
                    Thread.Sleep(200); // Laisser le moteur atteindre sa vitesse

                    lidar.Start("normal"); // Mode normal pour plus de fiabilité

                    var measureCount = 0;
                    var mesure = lidar.IterMeasures("normal");

                    foreach (var mes in mesure)
                    {
                        measureCount++;
                        if (measureCount > maxMeasuresPerScan) break;

                        double angle = mes.Item3;

                        // Normaliser l'angle entre 0 et 360
                        while (angle < 0) angle += 360;
                        while (angle >= 360) angle -= 360;

                        // Ignorer les angles de 90° à 270°
                        if (angle >= 90 && angle <= 270)
                            continue;

                        if (mes.Item4 > 0) // Ne conserver que les mesures avec distance non nulle
                        {
                            var closestIntAngle = (int)Math.Round(angle);
                            if (closestIntAngle == 360) closestIntAngle = 0;

                            angleMeasures[closestIntAngle] = (mes.Item1, mes.Item2, angle, mes.Item4);
                            newValidMeasures.Add((mes.Item1, mes.Item2, angle, mes.Item4));
                        }
                    }

                    validMeasures = newValidMeasures;
                    FillMissingAngles(); // Remplir les angles manquants
                    lidar.Stop();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Erreur lors de la mesure: {e.Message}");
                    throw;
                }
                finally
                {
                    if (isRunning)
                    {
                        // Laisser le moteur tourner pour les mesures suivantes
                    }
                    else
                    {
                        lidar.StopMotor();
                    }
                }
            }
        }

        private void FillMissingAngles()
        {
            var angleKeys = angleMeasures.Keys.ToList();
            if (angleKeys.Count < 2) return;

            angleKeys.Sort();

            for (int angle = 0; angle < 360; angle++)
            {
                if (angle >= 90 && angle <= 270) continue; // Ignorer les angles non pertinents
                if (angleMeasures.ContainsKey(angle)) continue;

                // Interpolation des angles manquants
                int lowerAngle = angleKeys.LastOrDefault(k => k < angle || (angle == 0 && k > 270));
                int upperAngle = angleKeys.FirstOrDefault(k => k > angle || (angle > 270 && k < 90));

                if (lowerAngle == -1 && angleKeys.Count > 0) lowerAngle = angleKeys.Last();
                if (upperAngle == -1 && angleKeys.Count > 0) upperAngle = angleKeys.First();

                if (lowerAngle != -1 && upperAngle != -1)
                {
                    if (angleMeasures.TryGetValue(lowerAngle, out var lowerValue) &&
                        angleMeasures.TryGetValue(upperAngle, out var upperValue))
                    {
                        double ratio = (angle - lowerAngle) / (double)(upperAngle > lowerAngle ? upperAngle - lowerAngle : (upperAngle + 360) - lowerAngle);
                        double distance = lowerValue.distance + ratio * (upperValue.distance - lowerValue.distance);
                        int quality = (int)(lowerValue.quality + ratio * (upperValue.quality - lowerValue.quality));

                        angleMeasures[angle] = (true, quality, angle, distance);
                    }
                    else
                    {
                        angleMeasures[angle] = (false, 1, angle, 500.0);
                    }
                }
                else
                {
                    angleMeasures[angle] = (false, 1, angle, 500.0);
                }
            }
        }

        private void GenerateImage(string filename)
        {
            try
            {
                var currentData = new Dictionary<int, (bool valid, int quality, double exactAngle, double distance)>();
                foreach (var pair in angleMeasures)
                {
                    currentData[pair.Key] = pair.Value;
                }

                LidarMapGenerator mapGenerator = new LidarMapGenerator(imageSize: 1000, scale: 0.1f);
                var map = mapGenerator.GenerateMap(currentData);
                mapGenerator.SaveMap(map, filename);
                Console.WriteLine($"Carte sauvegardée à {filename} avec {mapGenerator.Points} Points");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la génération de l'image: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopContinuousScan();
        }
    }
}