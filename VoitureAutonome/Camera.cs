using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace VoitureAutonome
{

    public class ColorAverage
    {
        public int GetAvergageColor(bool IsRed, string filename = "/home/covapsytours5/Documents/red50.png")
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
                        // Récupérer la couleur du pixel (Rgba32 permet d'accéder aux composantes R, G, B, A)
                        Rgba32 pixelColor = image[x, y];

                        // Ajouter la composante rouge (R) à la somme totale

                        if (IsRed) //SI rouge
                        {
                            total += pixelColor.R/255;
                            
                        }
                        else //Si vert
                        {
                            total += pixelColor.G/255;
                        }
                        
                        count++;
                       
                    }
                }

                // Calculer la moyenne de la composante rouge
                double averageR = (double)total / count;
                Console.WriteLine("Moyenne R: " + averageR);
                stopwatch.Stop();
                Console.WriteLine("Time elapsed: " + stopwatch.ElapsedMilliseconds);
            }

            return (int)total / count;
        }
    }
    
    
}