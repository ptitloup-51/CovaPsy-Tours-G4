using System;
using System.Device.Spi;
using System.Threading;
using System.Text;

namespace VoitureAutonome;

public class TestSpeed
{
    public string ReceivedMessage { get; private set; }
    public void Speed()
    {
        // Configuration du périphérique SPI
        var connectionSettings = new SpiConnectionSettings(0, 0); // Bus SPI 0 et chip select 0
        connectionSettings.ClockFrequency = 500000; // Fréquence SPI de 500 kHz
        connectionSettings.Mode = SpiMode.Mode0; // Mode SPI (le mode dépend de votre configuration STM32)

        // Ouvrir le périphérique SPI
        using (SpiDevice spiDevice = SpiDevice.Create(connectionSettings))
        {
            byte[] txBuffer = new byte[7]; // Buffer d'envoi vide
            byte[] rxBuffer = new byte[7]; // Buffer de réception
            while (true)
            {
                
                spiDevice.TransferFullDuplex(txBuffer, rxBuffer);
                
                // Convertir les données reçues en une chaîne de caractères
                ReceivedMessage = Encoding.ASCII.GetString(rxBuffer);

                // Affichage du message reçu
                Console.WriteLine($"Message reçu : {ReceivedMessage}");

                // Pause de 1 seconde
                Thread.Sleep(2000);

            }
        }
    }
    // Méthode pour démarrer le thread
    public void Start()
    {
        Thread speedThread = new Thread(new ThreadStart(Speed)); // Crée un thread pour la méthode Speed
        speedThread.IsBackground = true; // Le thread sera un thread en arrière-plan
        speedThread.Start(); // Démarre le thread
    }
}
public class Communication
{
    public void Com()
    {
        // Crée une instance de la classe TestSpeed et démarre le thread
        TestSpeed testSpeed = new TestSpeed();
        testSpeed.Start();
    }
}