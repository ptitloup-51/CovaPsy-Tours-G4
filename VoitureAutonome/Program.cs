namespace VoitureAutonome;

public class Program
{
    public static void Main(string[] args)
    { 
       // RemoteDebug debug = new RemoteDebug();
       //Direction dir = new Direction();
       
        Thrust thrust = new Thrust();
        thrust.SetSpeed(1); // à 50% 
        Thread.Sleep(1000);
        thrust.SetSpeed(10); // à 10% 
        Thread.Sleep(5000);
        thrust.SetSpeed(20); // à 10% 
        Thread.Sleep(5000);
        thrust.SetSpeed(30); // à 10% 
        Thread.Sleep(5000);
        thrust.Dispose(); //supprime l'objet
        
        
        Console.ReadLine();
       
    }
}