using System;
using System.Device.Pwm;
using System.Device.Gpio;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.Ports;
using System.Device.Spi;


namespace VoitureAutonome;

/// <summary>
/// Class <c>Thrust</c> Permet de gérer la vitesse de propulsion -100 -> 100% de vitesse en marche arrière et 100% = 100% de vitesse en marche avant
/// </summary>
public class Thrust
{
    private static PwmChannel pwmMotor;

    // Correction des valeurs pour correspondre au comportement attendu
    private const float _pwmMin = 8f / 100; // valeur min pour se déplacer
    private const float _pwmMax = 8.67f / 100; // Vitesse max
    private const float _pwmNeutral = 7.5f / 100; // 1.5 ms (Neutre)

    private double DutyCycle;

    private bool isInit;

    public Thrust()
    {
        pwmMotor = PwmChannel.Create(0, 0, 50, _pwmNeutral); // Initialisation avec la position neutre
        pwmMotor.Start();
        Thread.Sleep(4500);
        isInit = true;
    }

    /// <summary>
    /// Change la vitesse de la voiture, valeur entre 0% et 100%
    /// </summary>
    /// <param name="speed"></param>
    public void SetSpeed(int speed)
    {
        if (isInit == false)
        {
            Console.WriteLine("moteur pas init");
            return;
        }
        speed = Math.Clamp(speed, 0, 100); //
        
        // Mapper la vitesse de 0% à 100% vers la plage de PWM entre _pwmMin et _pwmMax
        DutyCycle = _pwmMin + (speed / 100.0) * (_pwmMax - _pwmMin);
        pwmMotor.DutyCycle = DutyCycle;

     //   Console.WriteLine($"Vitesse réglée à {speed}% -> PWM: {DutyCycle * 100:F2}%");
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
        
        float PWM = 6.7f / 100; // PWM neutre (tout droit)
        pwmSteering.DutyCycle = PWM;
        
        Console.WriteLine("Test des valeurs PWM pour la direction...");
        

        string input = "";

        while (true)
        {
            Console.Write("PWM: ");
            input = Console.ReadLine();

            if (input.ToLower() == "exit")
                break;

            if (input == "+")
            {
                PWM += 0.1f / 100;
            }
            else if (input == "-")
            {
                PWM -= 0.1f / 100;
            }
            else
            {
                PWM = float.Parse(input)/ 100.0f;
            }
            
            
            
            pwmSteering.DutyCycle = PWM;
            Console.WriteLine("PWM: " + PWM * 100);
            
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
    private const float _anglePwmMin = 3.8f / 100; // PWM min (gauche)
    private const float _anglePwmMax =  8.2f / 100; // PWM max (droite)
    private const float _anglePwmCenter = 5.5f / 100; // PWM neutre (tout droit)
    private const int _angleMax = 100; // Angle maximal en degrés

    private double DutyCycle;

    public Steering()
    {
        pwmSteering = PwmChannel.Create(0, 1, 50, _anglePwmCenter); // Initialisation en position neutre
        pwmSteering.Start();
        SetDirection(0);
    }

    /// <summary>
    /// Définit l'angle de direction en degrés (-18° à +18°)
    /// </summary>
    /// <param name="angle"></param>
    public void SetDirection(float angle)
    {
        angle = Math.Clamp(angle, -_angleMax, _angleMax);

        // Mapper l'angle (-18° à 18°) sur la plage PWM (6% à 9%)
        DutyCycle = _anglePwmCenter + (angle / (2.0 * _angleMax)) * (_anglePwmMax - _anglePwmMin);
        pwmSteering.DutyCycle = DutyCycle;

      //  Console.WriteLine($"Angle réglé à {angle}° -> PWM: {DutyCycle * 100:F2}%");
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
/*
public class AcquireSpeed;
{
    private SpiDevice spi;

    // Constructeur : Initialise la communication SPI
    public SpeedSensor(int busId = 0, int chipSelect = 0)
    {
        spi = SpiDevice.Create(new SpiConnectionSettings(busId, chipSelect)
        {
            ClockFrequency = 500000, // Fréquence SPI (ajuster si nécessaire)
            Mode = SpiMode.Mode0 // Mode de communication SPI (ajuster selon le STM32)
        });
    }

    // Méthode pour récupérer la vitesse depuis le STM32
    public int GetSpeed()
    {
        byte[] buffer = new byte[2]; // Tableau pour stocker les 2 octets de la vitesse
        spi.Read(buffer); // Lire les données SPI envoyées par le STM32

        // Convertir les 2 octets en un entier (Big Endian : MSB en premier)
        return (buffer[0] << 8) | buffer[1];
    }

    // Programme principal : Affiche la vitesse en boucle
    public static void Main()
    {
        var sensor = new SpeedSensor(); // Création de l'objet capteur

        while (true) // Boucle infinie pour lire la vitesse en continu
        {
            Console.WriteLine($"Vitesse: {sensor.GetSpeed()} RPM"); // Affichage de la vitesse
            System.Threading.Thread.Sleep(100); // Pause de 100ms avant la prochaine lecture
        }
    }
}
*/

 

