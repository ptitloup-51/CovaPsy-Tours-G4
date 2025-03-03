using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VoitureAutonome
{
    public class LidarMapGenerator
    {
        private int _imageSize;
        private int _center;
        private float _scale;
        public int Points;

        public LidarMapGenerator(int imageSize = 1000, float scale = 1.0f)
        {
            _imageSize = imageSize;
            _center = imageSize / 2;
            _scale = scale;
        }

        public Image<Rgba32> GenerateMap(Dictionary<int, (bool valid, int quality, double exactAngle, double distance)> angleMeasures)
        {
            Points = angleMeasures.Keys.Count;
            // Créer une image vide
            using Image<Rgba32> image = new Image<Rgba32>(_imageSize, _imageSize);

            // Remplir l'image avec un fond blanc
            image.Mutate<Rgba32>(ctx => ctx.BackgroundColor(Color.White));

            // Charger une police pour le texte
            var fontCollection = new FontCollection();
            var fontFamily = fontCollection.AddSystemFonts().Families.FirstOrDefault();
            var font = fontFamily.CreateFont(12); // Taille de la police

            // Dessiner les angles (tous les 30 degrés)
            for (int angle = 0; angle < 360; angle += 30)
            {
                double radians = angle * Math.PI / 180.0;
                int x = _center + (int)(Math.Cos(radians) * _imageSize / 2 * 0.9);
                int y = _center - (int)(Math.Sin(radians) * _imageSize / 2 * 0.9); // Inversion de l'axe Y

                // Dessiner une ligne du centre à l'angle
                var startPoint = new PointF(_center, _center);
                var endPoint = new PointF(x, y);
                image.Mutate<Rgba32>(ctx => ctx.DrawLine(
                    Color.LightGray, // Couleur des lignes d'angle
                    1f, // Épaisseur de la ligne
                    startPoint, endPoint
                ));

                // Ajouter un texte pour l'angle
                var text = $"{angle}°";
                var textOptions = new RichTextOptions(font)
                {
                    Origin = new PointF(x, y),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                image.Mutate<Rgba32>(ctx => ctx.DrawText(
                    textOptions,
                    text,
                    Color.Gray // Couleur du texte
                ));
            }

            // Dessiner les points du LIDAR
            foreach (var kvp in angleMeasures)
            {
                int angle = kvp.Key;
                double distance = kvp.Value.distance;

                // Convertir l'angle et la distance en coordonnées cartésiennes
                double radians = angle * Math.PI / 180.0;
                double x = distance * Math.Cos(radians);
                double y = distance * Math.Sin(radians);

                // Ajuster les coordonnées en fonction de l'échelle et du centre de l'image
                int pixelX = _center + (int)(x * _scale);
                int pixelY = _center - (int)(y * _scale); // Inversion de l'axe Y pour l'affichage

                // Dessiner un point sur l'image
                if (pixelX >= 0 && pixelX < _imageSize && pixelY >= 0 && pixelY < _imageSize)
                {
                    image[pixelX, pixelY] = Color.Black;
                }
            }

            return image.Clone(); // Retourner une copie de l'image
        }

        public void SaveMap(Image<Rgba32> image, string filePath)
        {
            image.Save(filePath); // Sauvegarder l'image au format PNG
        }
    }
}