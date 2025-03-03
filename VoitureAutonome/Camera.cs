using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace VoitureAutonome
{
    public class Camera
    {
        public void StartCapture()
        {
            VideoCapture capture = new VideoCapture();  // Initialise un objet VideoCapture pour capturer la vidéo.
            var lowerGreen = new ScalarArray(new MCvScalar(35, 50, 50));  // Borne inférieure en HSV
            var upperGreen = new ScalarArray(new MCvScalar(85, 255, 255)); // Borne supérieure en HSV

            try
            {
                while (true)  // Boucle infinie pour capturer et traiter les frames.
                {
                    var frame = capture.QueryFrame();  // Capture un frame vidéo.
                    if (frame == null) break;  // Si aucun frame n'est capturé, sort de la boucle.

                    var hsv = new Mat();  // Crée une matrice pour stocker l'image en HSV.
                    CvInvoke.CvtColor(frame, hsv, ColorConversion.Bgr2Hsv);  // Convertit l'image de BGR à HSV.

                    var mask = new Mat();  // Crée une matrice pour le masque.
                    CvInvoke.InRange(hsv, lowerGreen, upperGreen, mask);  // Crée un masque pour les couleurs dans la plage verte.

                    var filteredFrame = new Mat();  // Crée une matrice pour stocker l'image filtrée.
                    CvInvoke.BitwiseAnd(frame, frame, filteredFrame, mask);  // Applique le masque à l'image originale.

                    CvInvoke.Imshow("Original", frame);  // Affiche l'image originale.
                    CvInvoke.Imshow("Filtre Vert", filteredFrame);  // Affiche l'image filtrée.

                    if (CvInvoke.WaitKey(1) == 'q')  // Attend une touche, quitte si 'q' est pressé.
                        break;
                }
            }
            finally
            {
                capture.Dispose();  // Libère les ressources utilisées par VideoCapture.
                CvInvoke.DestroyAllWindows();  // Ferme toutes les fenêtres ouvertes par OpenCV.
            }
        }
    }
}