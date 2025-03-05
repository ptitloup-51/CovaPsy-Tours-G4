using System;
using System.Device.Spi;
using System.Threading;
using System.Text;

namespace VoitureAutonome;

public class TestSpeed
{
    public void Speed()
    {
        // Configuration du périphérique SPI
        var connectionSettings = new SpiConnectionSettings(0, 0); // Bus SPI 0 et chip select 0
        connectionSettings.ClockFrequency = 500000; // Fréquence SPI de 500 kHz
        connectionSettings.Mode = SpiMode.Mode0; // Mode SPI (le mode dépend de votre configuration STM32)

        // Ouvrir le périphérique SPI
        using (SpiDevice spiDevice = SpiDevice.Create(connectionSettings))
        {
            byte[] rxBuffer = new byte[7]; // Buffer de réception (4 octets pour un float)
            while (true)
            {
                
                spiDevice.Read(rxBuffer); // Lecture des données envoyées par la STM32
                
                // Convertir les données reçues en une chaîne de caractères
                string message = Encoding.ASCII.GetString(rxBuffer);

                // Affichage du message reçu
                Console.WriteLine($"Message reçu : {message}");

                // Pause de 1 seconde
                Thread.Sleep(1000);

            }
        }
    }    
}