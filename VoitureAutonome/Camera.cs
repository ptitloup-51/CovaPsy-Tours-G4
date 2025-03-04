using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace VoitureAutonome
{
    public class Camera
    {
        // Paramètres ajustables pour la détection des couleurs
        public MCvScalar LowerGreenHsv { get; set; } = new MCvScalar(35, 50, 50);
        public MCvScalar UpperGreenHsv { get; set; } = new MCvScalar(85, 255, 255);
        
        // Propriétés pour les résultats de détection
        public Rectangle DetectedGreenObject { get; private set; }
        public double DetectedGreenArea { get; private set; }
        
        // Événements pour notifier d'autres composants
        public event EventHandler<ObjectDetectedEventArgs> ObjectDetected;
        
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isCapturing = false;
        
        /// <summary>
        /// Démarre la capture vidéo dans un thread séparé
        /// </summary>
        public async Task StartCaptureAsync()
        {
            if (_isCapturing) return;
            
            _isCapturing = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            await Task.Run(() => CaptureLoop(_cancellationTokenSource.Token));
        }
        
        /// <summary>
        /// Arrête la capture vidéo
        /// </summary>
        public void StopCapture()
        {
            if (!_isCapturing) return;
            
            _cancellationTokenSource?.Cancel();
            _isCapturing = false;
        }
        
        /// <summary>
        /// Boucle principale de capture et traitement d'images
        /// </summary>
        private void CaptureLoop(CancellationToken cancellationToken)
        {
            VideoCapture capture = new VideoCapture(0); // Utilise la première caméra disponible
            
            // Optimisation: réduire la résolution si nécessaire pour les performances
            capture.Set(CapProp.FrameWidth, 640);
            capture.Set(CapProp.FrameHeight, 480);
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var frame = capture.QueryFrame();  // Capture un frame vidéo
                    if (frame == null) break;
                    
                    ProcessFrame(frame);
                    
                    // Affiche l'image avec les résultats de détection
                    using (var displayFrame = frame.Clone())
                    {
                        if (DetectedGreenObject.Width > 0 && DetectedGreenObject.Height > 0)
                        {
                            CvInvoke.Rectangle(displayFrame, DetectedGreenObject, new MCvScalar(0, 0, 255), 2);
                            CvInvoke.PutText(displayFrame, $"Area: {DetectedGreenArea:F2}", 
                                new Point(10, 30), FontFace.HersheyComplex, 1.0, new MCvScalar(0, 0, 255));
                        }
                        
                        CvInvoke.Imshow("Détection", displayFrame);
                    }
                    
                    if (CvInvoke.WaitKey(1) == 'q')  // Quitte si 'q' est pressé
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Une erreur s'est produite : " + ex.Message);
            }
            finally
            {
                _isCapturing = false;
                capture.Dispose();
                CvInvoke.DestroyAllWindows();
            }
        }
        
        /// <summary>
        /// Traite une image pour détecter les objets verts
        /// </summary>
        private void ProcessFrame(Mat frame)
        {
            using (var hsv = new Mat())
            using (var mask = new Mat())
            using (var filteredFrame = new Mat())
            using (var hierarchy = new Mat())
            using (var contours = new VectorOfVectorOfPoint())
            {
                // Conversion et filtrage des couleurs
                CvInvoke.CvtColor(frame, hsv, ColorConversion.Bgr2Hsv);
                var lowerGreen = new ScalarArray(LowerGreenHsv);
                var upperGreen = new ScalarArray(UpperGreenHsv);
                CvInvoke.InRange(hsv, lowerGreen, upperGreen, mask);
                
                // Réduction du bruit
                CvInvoke.Erode(mask, mask, null, new Point(-1, -1), 2, BorderType.Default, new MCvScalar(0));
                CvInvoke.Dilate(mask, mask, null, new Point(-1, -1), 2, BorderType.Default, new MCvScalar(0));
                
                CvInvoke.BitwiseAnd(frame, frame, filteredFrame, mask);
                
                // Affiche les résultats intermédiaires
                CvInvoke.Imshow("Masque Vert", mask);
                CvInvoke.Imshow("Filtre Vert", filteredFrame);
                
                // Trouver les contours des objets verts
                CvInvoke.FindContours(mask, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                
                // Analyse des contours pour trouver le plus grand objet vert
                Rectangle largestObjectRect = new Rectangle();
                double largestArea = 0;
                
                for (int i = 0; i < contours.Size; i++)
                {
                    var contour = contours[i];
                    double area = CvInvoke.ContourArea(contour);
                    
                    if (area > 500) // Ignorer les petits objets (bruit)
                    {
                        Rectangle rect = CvInvoke.BoundingRectangle(contour);
                        
                        if (area > largestArea)
                        {
                            largestArea = area;
                            largestObjectRect = rect;
                        }
                    }
                }
                
                // Mise à jour des propriétés
                DetectedGreenObject = largestObjectRect;
                DetectedGreenArea = largestArea;
                
                // Notification d'autres composants si un objet est détecté
                if (largestArea > 0)
                {
                    ObjectDetected?.Invoke(this, new ObjectDetectedEventArgs
                    {
                        ObjectRect = largestObjectRect,
                        ObjectArea = largestArea,
                        ObjectCenter = new Point(
                            largestObjectRect.X + largestObjectRect.Width / 2,
                            largestObjectRect.Y + largestObjectRect.Height / 2
                        )
                    });
                }
            }
        }
    }
    
    /// <summary>
    /// Arguments d'événement pour la détection d'objets
    /// </summary>
    public class ObjectDetectedEventArgs : EventArgs
    {
        public Rectangle ObjectRect { get; set; }
        public double ObjectArea { get; set; }
        public Point ObjectCenter { get; set; }
    }
}