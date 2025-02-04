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
    private int _trajectoryPin;
    private GpioController _gpio;
    
    public Thrust(int pwmChip, int pwmChannel, int trajectoryPin, int frequency = 1000)
    {
        _pwm = PwmChannel.Create(pwmChip, pwmChannel, frequency, 0.0);
        _trajectoryPin = trajectoryPin;
        _gpio = new GpioController();
        _gpio.OpenPin(_trajectoryPin, PinMode.Output);
    }
    public void SetSpeed(int speed) // Plage de -100 à 100
    {
        speed = Math.Clamp(speed, -100, 100);
        double dutyCycle = Math.Abs(speed) / 100.0; // Conversion en 0 - 1
        
        if (speed == 0)
        {
            _pwm.Stop();
        }
        else
        {
            _gpio.Write(_trajectoryPin, speed > 0 ? PinValue.High : PinValue.Low);
            _pwm.DutyCycle = dutyCycle;
            _pwm.Start();
        }
    }

    public void Stop()
    {
        _pwm.Stop();
    }
}

/// <summary>
/// Class <c>Direction</c> Permet de gérer l'angle de braquage entre 0 et 180°, 90° étant la direction en avant
/// </summary>


public class Direction
{
    // Paramètres de départ
    int direction = -1;  // 1 = gauche, -1 = droite
    float anglePwmMin = 1000f; // Min
    float anglePwmMax = 1000f; // Max
    float anglePwmCentre = 1f; // Centre
    float angleDegreMax = 90; // Angle max en degrés
    float angleDegre = 0; // Angle actuel

    // Déclaration du PWM
    static PwmChannel pwm = PwmChannel.Create(13, 1, 50);  
    // Gpio 13, Canal 1, 50 Hz => initialement mis sur Gpio 0 mais contradictoire avec le schéma structurel

    public Direction()
    {
        pwm.Start();  // Démarre la PWM
        SetDirectionDegre(angleDegre);  // Initialise au centre
    }

    // Fonction pour régler l'angle du servo
    void SetDirectionDegre(float angleDegre)
    {
        // Calcul de l'angle PWM en fonction de l'angle en degrés
        float anglePwm = anglePwmCentre + direction * (anglePwmMax - anglePwmMin) * angleDegre / (2 * angleDegreMax);
        // Limite l'angle PWM aux butées
        if (anglePwm > anglePwmMax) anglePwm = anglePwmMax;
        if (anglePwm < anglePwmMin) anglePwm = anglePwmMin;
        // Conversion en pourcentage pour le PWM
        pwm.DutyCycle = (anglePwm - anglePwmMin) / (anglePwmMax - anglePwmMin); // Pourcentage entre 0 et 1
    }
    // Ajuste la valeur du centre après modification des butées
    void AjusterCentre()
    {
        anglePwmCentre = (anglePwmMax + anglePwmMin) / 2;
    }
}
