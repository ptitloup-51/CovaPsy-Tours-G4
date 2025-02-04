using System;
using System.Device.Pwm;
using System.Device.Gpio;
using System.Threading;

namespace VoitureAutonome;

/// <summary>
/// Class <c>Thrust</c> Permet de gérer la vitesse de propulsion -100 -> 100% de vitesse en marche arrière et 100% = 100% de vitesse en marche avant
/// </summary>
public class Thrust
{
    static PwmChannel pwmMotor;
    static PwmChannel pwmDirection;
    
    // Paramètres moteur
    static double pwmStop = 8.10;
    static double pointMort = 0.46;
    static double deltaPwmMax = 1.5;
    static double vitesseMaxSoft = 2.0;
    static double vitesseMaxHard = 8.0;
    static int directionProp = -1;
    
    static void SetVitesse(double vitesse)
    {
        double dutyCycle = pwmStop;
        if (vitesse > 0)
        {
            dutyCycle += directionProp * (pointMort + vitesse * (deltaPwmMax / vitesseMaxHard));
        }
        else if (vitesse < 0)
        {
            dutyCycle -= directionProp * (pointMort - vitesse * (deltaPwmMax / vitesseMaxHard));
        }

        pwmMotor.DutyCycle = dutyCycle / 100.0;
        Console.WriteLine($"Vitesse réglée : {vitesse} m/s, Duty Cycle : {dutyCycle}%");
    }
}

/// <summary>
/// Class <c>Direction</c> Permet de gérer l'angle de braquage entre 0 et 180°, 90° étant la direction en avant
/// </summary>

/*
public class Direction
{
    // Paramètres de départ
    private int direction = -1;  // 1 = gauche, -1 = droite
    private float anglePwmMin = 6.6f; // Min
    private float anglePwmMax = 8.9f; // Max
    private float anglePwmCentre = 7.75f; // Centre
    private float angleDegreMax = 18; // Angle max en degrés
    private float angleDegre = 0; // Angle actuel
    
    // Initialisation du PWM
    static PwmChannel pwm = PwmChannel.Create(0, 1, 50); // Canal 1, 50 Hz

    static void Main()
    {
        pwm.Start();
        SetDirectionDegre(angleDegre); // Initialiser au centre

        Console.WriteLine("Réglage des butées, Q pour quitter");
        Console.WriteLine("Valeur numérique pour tester un angle de direction");
        Console.WriteLine("I pour inverser droite et gauche");
        Console.WriteLine("g pour diminuer la butée gauche et G pour l'augmenter");
        Console.WriteLine("d pour diminuer la butée droite et D pour l'augmenter");

        while (true)
        {
            Console.Write("Angle, I, g, G, d, D ? ");
            string input = Console.ReadLine();

            if (input == "Q" || input == "q")
                break;

            if (int.TryParse(input, out int angle))
            {
                SetDirectionDegre(angle);
            }
            else
            {
                switch (input)
                {
                    case "I":
                        direction = -direction;
                        Console.WriteLine("Nouvelle direction : " + direction);
                        break;
                    case "g":
                        if (direction == 1)
                            anglePwmMax -= 0.1;
                        else
                            anglePwmMin += 0.1;
                        AjusterCentre();
                        SetDirectionDegre(18);
                        break;
                    case "G":
                        if (direction == 1)
                            anglePwmMax += 0.1;
                        else
                            anglePwmMin -= 0.1;
                        AjusterCentre();
                        SetDirectionDegre(18);
                        break;
                    case "d":
                        if (direction == -1)
                            anglePwmMax -= 0.1;
                        else
                            anglePwmMin += 0.1;
                        AjusterCentre();
                        SetDirectionDegre(-18);
                        break;
                    case "D":
                        if (direction == -1)
                            anglePwmMax += 0.1;
                        else
                            anglePwmMin -= 0.1;
                        AjusterCentre();
                        SetDirectionDegre(-18);
                        break;
                    default:
                        Console.WriteLine("Commande non reconnue.");
                        break;
                }
            }
        }

        Console.WriteLine("Nouvelles valeurs:");
        Console.WriteLine($"Direction : {direction}");
        Console.WriteLine($"anglePwmMin : {anglePwmMin}");
        Console.WriteLine($"anglePwmMax : {anglePwmMax}");
        Console.WriteLine($"anglePwmCentre : {anglePwmCentre}");

        pwm.Stop();
    }

    // Fonction pour régler l'angle du servo
    static void SetDirectionDegre(double angleDegre)
    {
        double anglePwm = anglePwmCentre + direction * (anglePwmMax - anglePwmMin) * angleDegre / (2 * angleDegreMax);
        
        if (anglePwm > anglePwmMax) anglePwm = anglePwmMax;
        if (anglePwm < anglePwmMin) anglePwm = anglePwmMin;

        pwm.DutyCycle = anglePwm / 100.0; // Conversion en pourcentage
    }

    // Ajuste la valeur du centre après modification des butées
    static void AjusterCentre()
    {
        anglePwmCentre = (anglePwmMax + anglePwmMin) / 2;
    }
}
*/