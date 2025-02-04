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
    private PwmChannel _pwm;
    private int _enablePin;
    private int _directionPin;
    private GpioController _gpio;
    
    public Thrust(int pwmChip, int pwmChannel, int enablePin, int directionPin, int frequency = 1000)
    {
        _pwm = PwmChannel.Create(pwmChip, pwmChannel, frequency, 0.0);
        _enablePin = enablePin;
        _directionPin = directionPin;
        _gpio = new GpioController();
        
        _gpio.OpenPin(_enablePin, PinMode.Output);
        _gpio.OpenPin(_directionPin, PinMode.Output);
    }
    public void SetSpeed(int speed) // Plage de -100 à 100
    {
        speed = Math.Clamp(speed, -100, 100);
        double dutyCycle = Math.Abs(speed) / 100.0; // Conversion en 0 - 1
        
        if (speed == 0)
        {
            _pwm.Stop();
            _gpio.Write(_enablePin, PinValue.Low);
        }
        else
        {
            _gpio.Write(_directionPin, speed > 0 ? PinValue.High : PinValue.Low);
            _pwm.DutyCycle = dutyCycle;
            _pwm.Start();
            _gpio.Write(_enablePin, PinValue.High);
        }
    }

    public void Stop()
    {
        _pwm.Stop();
        _gpio.Write(_enablePin, PinValue.Low);
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