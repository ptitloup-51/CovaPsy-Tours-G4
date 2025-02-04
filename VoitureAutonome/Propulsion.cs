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
public class Direction
{
    
}