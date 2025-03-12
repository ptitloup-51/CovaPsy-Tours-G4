using System.Device.Spi;

namespace VoitureAutonome;

public class STMCom
{
    private bool IsListening = false;
    public STMCom()
    {
        // Configuration du périphérique SPI
        var connectionSettings = new SpiConnectionSettings(0, 1); // Bus SPI 0 et chip select 0
        connectionSettings.ClockFrequency = 1000000; // Fréquence SPI de 1 MHz
        connectionSettings.Mode = SpiMode.Mode0; // Mode SPI (le mode dépend de votre configuration STM32)
        
        
        Task.Run(() => Listen(connectionSettings));
        
    }

    /// <summary>
    /// Permet d'envoyer une commande à la STM 32
    /// </summary>
    /// <param name="commande"> Nom de la commande</param>
    /// <param name="content"> Argument de la commande</param>
    public void SendCommande(string commande, string content)
    {
        
    }

    public void Listen(SpiConnectionSettings settings)
    {
        IsListening = true;
        while (IsListening)
        {
            using (SpiDevice spiDevice = SpiDevice.Create(settings))
            {
                byte[] txBuffer = new byte [4]; // Octet de requête
                byte[] rxBuffer = new byte [4]; // Buffer de réception

                // Réinitialisation du buffer de réception
                Array.Clear(rxBuffer, 0, rxBuffer.Length);

                Thread.Sleep(10);

                spiDevice.TransferFullDuplex(txBuffer, rxBuffer);

                // Convertir les 4 octets reçus en float
                float vitesse = BitConverter.ToSingle(rxBuffer, 0);
              //  Console.WriteLine($"Vitesse reçue : {vitesse:F2} m/s");
                
                OnMessageReceive?.Invoke(vitesse.ToString());

                // Pause de 1s
                Thread.Sleep(100);

            }
        }
        IsListening = false;
        
    }
    
    public delegate void Callaback(string message);

    // Définition de l'événement basé sur ce délégué
    public event Callaback? OnMessageReceive;
    
    
    
    
    
}