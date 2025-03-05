using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;

public class LidarMapGenerator
{
    private int _imageSize;
    private int _center;
    private float _scale;

    public LidarMapGenerator(int imageSize = 1000, float scale = 1.0f)
    {
        _imageSize = imageSize;
        _center = imageSize / 2;
        _scale = scale;
    }

    public Image<Rgba32> GenerateMap(Dictionary<int, (bool valid, int quality, double exactAngle, double distance)> angleMeasures)
    {
        var image = new Image<Rgba32>(_imageSize, _imageSize, Color.White);

        // Dessiner les axes et angles
        for (int angle = 0; angle < 360; angle += 30)
        {
            double radians = angle * Math.PI / 180.0;
            int x = _center + (int)(Math.Cos(radians) * _imageSize / 2 * 0.9);
            int y = _center - (int)(Math.Sin(radians) * _imageSize / 2 * 0.9);

            image.Mutate(ctx => ctx.DrawLine(Color.LightGray, 1f, new PointF(_center, _center), new PointF(x, y)));
        }

        // Dessiner les points du LIDAR
        foreach (var kvp in angleMeasures)
        {
            int angle = kvp.Key;
            double distance = kvp.Value.distance;

            double radians = angle * Math.PI / 180.0;
            int pixelX = _center + (int)(distance * Math.Cos(radians) * _scale);
            int pixelY = _center - (int)(distance * Math.Sin(radians) * _scale);

            if (pixelX >= 0 && pixelX < _imageSize && pixelY >= 0 && pixelY < _imageSize)
            {
                image.Mutate(ctx => ctx.Fill(Color.Black, new EllipsePolygon(new PointF(pixelX, pixelY), 3f)));
            }
        }

        return image;
    }
}