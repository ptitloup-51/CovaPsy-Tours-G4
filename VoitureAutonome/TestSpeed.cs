using System;
using System.Device.Spi;
using System.Threading;
using System.Text;

namespace VoitureAutonome;

public class TestSpeed
{
    public static void Speed()
    {
        // Configuration du périphérique SPI
        var connectionSettings = new SpiConnectionSettings(0, 0); // Bus SPI 0 et chip select 0
        connectionSettings.ClockFrequency = 500000; // Fréquence SPI de 500 kHz
        connectionSettings.Mode = SpiMode.Mode0; // Mode SPI (le mode dépend de votre configuration STM32)

        // Ouvrir le périphérique SPI
        using (SpiDevice spiDevice = SpiDevice.Create(connectionSettings))
        {
            while (true)
            {
                byte[] rxBuffer = new byte[4]; // Buffer de réception (4 octets pour un float)

                spiDevice.Read(rxBuffer); // Lecture des données envoyées par la STM32

                float vitesse = BitConverter.ToSingle(rxBuffer, 0); // Conversion des octets en float

                Console.WriteLine($"Vitesse reçue: {vitesse:F2} m/s");

                Thread.Sleep(1000); // Pause d'une seconde avant la prochaine lecture
            }
        }
    }    
}