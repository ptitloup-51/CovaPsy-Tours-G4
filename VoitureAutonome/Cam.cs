using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace VoitureAutonome
{
    public class ColorAverage
    {
        public int GetAverageColor(bool IsRed, string filename)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int count = 0;
            float total = 0;

            using (Image<Rgba32> image = Image.Load<Rgba32>(filename))
            {
                // Parcourir tous les pixels de l'image
                for (int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        // Récupérer la couleur du pixel
                        Rgba32 pixelColor = image[x, y];

                        // Ajouter la composante rouge ou verte à la somme totale
                        if (IsRed) // Si rouge
                        {
                            total += pixelColor.R / 255f;
                        }
                        else // Si vert
                        {
                            total += pixelColor.G / 255f;
                        }

                        count++;
                    }
                }

                // Calculer la moyenne de la composante
                double average = (double)total / count;
                Console.WriteLine("Moyenne: " + average);
                stopwatch.Stop();
                Console.WriteLine("Temps écoulé: " + stopwatch.ElapsedMilliseconds + " ms");
            }

            return (int)(total / count * 100); // Retourne un pourcentage
        }

        public void CaptureAndProcess(bool IsRed)
        {
            string imageFolder = "/home/covapsytours5/Documents/images";
            string filename = Path.Combine(imageFolder, $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
            
            // Utiliser la commande raspistill pour capturer une image
            string rpicamCommand =  $"rpicam-jpeg -o  {filename} -t 1000"; // -t 1000 : temps de capture en ms
            ExecuteShellCommand(rpicamCommand);

            if (File.Exists(filename))
            {
                Console.WriteLine("Capture sauvegardée: " + filename);

                // Traiter l'image
                int percentage = GetAverageColor(IsRed, filename);
                Console.WriteLine("Pourcentage de pixels " + (IsRed ? "rouges" : "verts") + ": " + percentage + "%");

                // Supprimer l'image après traitement
                File.Delete(filename);
                Console.WriteLine("Capture supprimée: " + filename);
            }
            else
            {
                Console.WriteLine("Erreur: pas d'image capturée.");
            }
        }

        private void ExecuteShellCommand(string command)
        {
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine("Sortie: " + output);
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("Erreur: " + error);
                }
            }
        }
    }
}