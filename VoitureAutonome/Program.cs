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
        
        var lidarScanner = new RPLidarScanner("/dev/ttyUSB0");
        lidarScanner.StartScanning();

        // Boucle principale du programme
        while (true)
        {
            var scanData = lidarScanner.GetLatestScan();
            
            scanData.TryGetValue(0, out var lidarData);
            Console.WriteLine($"LIDAR DATA: {lidarData}");
            /*
            // Affichage ou traitement des données
            foreach (var (angle, distance) in scanData)
                Console.WriteLine($"Angle: {angle}, Distance: {distance} mm");
                */

            Thread.Sleep(100); // Éviter de spammer la console
        }

        // Pour arrêter proprement
        lidarScanner.StopScanning();
        
         
        /*
        TestSpeed testSpeed = new TestSpeed();
        testSpeed.Speed();
        */
        


    }
}