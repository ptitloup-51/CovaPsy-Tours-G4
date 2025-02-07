using System;
using System.Device.Pwm;
using System.Device.Gpio;
using System.Runtime.InteropServices;
using System.Threading;


namespace VoitureAutonome;

/// <summary>
/// Class <c>Thrust</c> Permet de gérer la vitesse de propulsion -100 -> 100% de vitesse en marche arrière et 100% = 100% de vitesse en marche avant
/// </summary>
public class Thrust
{
    private static PwmChannel pwmMotor;

    // Correction des valeurs pour correspondre au comportement attendu
    private const float _pwmMin = 8.10f / 100; // valeur min pour se déplacer
    private const float _pwmMax = 8.67f / 100; // Vitesse max

    private double DutyCycle;

    public Thrust()
    {
        pwmMotor = PwmChannel.Create(0, 0, 50, _pwmMin); // Initialisation avec la position neutre
        pwmMotor.Start();
    }

    /// <summary>
    /// Change la vitesse de la voiture, valeur entre 0% et 100%
    /// </summary>
    /// <param name="speed"></param>
    public void SetSpeed(int speed)
    {
        speed = Math.Clamp(speed, 0, 100); //
        
        // Mapper la vitesse de 0% à 100% vers la plage de PWM entre _pwmMin et _pwmMax
        DutyCycle = _pwmMin + (speed / 100.0) * (_pwmMax - _pwmMin);
        pwmMotor.DutyCycle = DutyCycle;

        Console.WriteLine($"Vitesse réglée à {speed}% -> PWM: {DutyCycle * 100:F2}%");
    }

    public void Stop()
    {
        pwmMotor.DutyCycle = _pwmMin; // Revenir à la position neutre
        Console.WriteLine("Moteur arrêté (position neutre).");
    }

    /// <summary>
    /// Permet de proprement supprimer l'objet
    /// </summary>
    public void Dispose()
    {
        pwmMotor.Stop();
        pwmMotor.Dispose();
        Console.WriteLine("PWM arrêté proprement.");
    }
}



/// <summary>
/// Class <c>Direction</c> Permet de gérer l'angle de braquage entre 0 et 180°, 90° étant la direction en avant
/// </summary>


//permet d'identifier les valeurs de PWM
class SteeringTest
{
    public void Test()
    {
        PwmChannel pwmSteering = PwmChannel.Create(0, 1, 50, 0.075); // Fréquence de 50 Hz, valeur initiale neutre
        pwmSteering.Start();

        Console.WriteLine("Test des valeurs PWM pour la direction...");
        Console.WriteLine(
            "Entrez une valeur de PWM en pourcentage (ex: 6.0 pour gauche, 7.5 pour neutre, 9.0 pour droite). Tapez 'exit' pour quitter.");

        while (true)
        {
            Console.Write("PWM (%): ");
            string input = Console.ReadLine();

            if (input.ToLower() == "exit")
                break;

            if (double.TryParse(input, out double pwmValue))
            {
                double dutyCycle = pwmValue / 100.0; // Conversion en fraction
                pwmSteering.DutyCycle = dutyCycle;
                Console.WriteLine($"PWM réglé à {pwmValue}%");
            }
            else
            {
                Console.WriteLine("Valeur invalide, veuillez entrer un nombre.");
            }
        }

        pwmSteering.Stop();
        pwmSteering.Dispose();
        Console.WriteLine("Test terminé.");
    }
}

public class Steering
{
    private static PwmChannel pwmSteering;

    // Paramètres de la direction
    private const float _anglePwmMin = 6.0f / 100; // PWM min (gauche)
    private const float _anglePwmMax = 12.0f / 100; // PWM max (droite)
    private const float _anglePwmCenter = 8f / 100; // PWM neutre (tout droit)
    private const int _angleMax = 70; // Angle maximal en degrés

    private double DutyCycle;

    public Steering()
    {
        pwmSteering = PwmChannel.Create(0, 1, 50, _anglePwmCenter); // Initialisation en position neutre
        pwmSteering.Start();
    }

    /// <summary>
    /// Définit l'angle de direction en degrés (-18° à +18°)
    /// </summary>
    /// <param name="angle"></param>
    public void SetDirection(int angle)
    {
        angle = Math.Clamp(angle, -_angleMax, _angleMax);

        // Mapper l'angle (-18° à 18°) sur la plage PWM (6% à 9%)
        DutyCycle = _anglePwmCenter + (angle / (2.0 * _angleMax)) * (_anglePwmMax - _anglePwmMin);
        pwmSteering.DutyCycle = DutyCycle;

        Console.WriteLine($"Angle réglé à {angle}° -> PWM: {DutyCycle * 100:F2}%");
    }

    public void Center()
    {
        pwmSteering.DutyCycle = _anglePwmCenter; // Revenir à la position neutre
        Console.WriteLine("Direction centrée.");
    }

    /// <summary>
    /// Permet de proprement supprimer l'objet
    /// </summary>
    public void Dispose()
    {
        pwmSteering.Stop();
        pwmSteering.Dispose();
        Console.WriteLine("PWM direction arrêté proprement.");
    }
}


 

