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
        var connectionSettings = new SpiConnectionSettings(0, 1); // Bus SPI 0 et chip select 0
        connectionSettings.ClockFrequency = 1000000; // Fréquence SPI de 1 MHz
        connectionSettings.Mode = SpiMode.Mode0; // Mode SPI (le mode dépend de votre configuration STM32)

        // Ouvrir le périphérique SPI
        using (SpiDevice spiDevice = SpiDevice.Create(connectionSettings))
        {
            byte[] txBuffer = {0x55, 0x55, 0, 2, 4, 6, 8}; // Buffer d'envoi
            byte[] rxBuffer = new byte[txBuffer.Length]; // Buffer de réception
            
            while (true)
            {
                // Réinitialisation du buffer de réception
                Array.Clear(rxBuffer, 0, rxBuffer.Length);
                
                Thread.Sleep(10);
                
                spiDevice.TransferFullDuplex(txBuffer, rxBuffer);

                // Convertir les octets reçus en string
                string message = Encoding.ASCII.GetString(rxBuffer);
                
                // Affichage du message reçu
                Console.WriteLine($"Message reçu : {message.Trim()}");
                Console.WriteLine("Reçu (HEX): " + BitConverter.ToString(rxBuffer));
                Console.WriteLine("Reçu (Texte): " + Encoding.ASCII.GetString(rxBuffer));

                // Pause de 500ms
                Thread.Sleep(1000);

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