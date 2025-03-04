using System;
using System.Device.Spi;
using System.Threading;

namespace VoitureAutonome;

public class TestSpeed
{
    static void Speed()
    {
        // Configuration du périphérique SPI
        var connectionSettings = new SpiConnectionSettings(0, 0); // Bus SPI 0 et chip select 0
        connectionSettings.ClockFrequency = 500000; // Fréquence SPI de 500 kHz
        connectionSettings.Mode = SpiMode.Mode0; // Mode SPI (le mode dépend de votre configuration STM32)

        // Ouvrir le périphérique SPI
        using (SpiDevice spiDevice = SpiDevice.Create(connectionSettings))
        {
            // Buffer de transmission et réception
            byte[] txBuffer = new byte[6] { 0x55, 0x55, 0, 0, 0, 0 }; // Les données envoyées au STM32
            byte[] rxBuffer = new byte[6]; // Buffer pour la réponse du STM32

            while (true)
            {
                // Envoi des données SPI
                spiDevice.Write(txBuffer); // Envoie le buffer à l'appareil

                // Lecture des données SPI
                spiDevice.Read(rxBuffer); // Lit la réponse dans le buffer de réception

                // Extraction de la vitesse mesurée à partir du buffer (supposons qu'elle soit dans les 4 premiers octets)
                int vitesseMesuree = BitConverter.ToInt32(rxBuffer, 0);

                // Affichage de la vitesse mesurée
                Console.WriteLine($"Vitesse Mesurée: {vitesseMesuree} m/s");

                // Pause de 1 seconde
                Thread.Sleep(1000);
            }
        }
    }    
}