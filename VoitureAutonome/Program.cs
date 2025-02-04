namespace VoitureAutonome;

public class Program
{
    public static void Main(string[] args)
    { 
       // RemoteDebug debug = new RemoteDebug();
       //Direction dir = new Direction();
       
        Thrust thrust = new Thrust();
        thrust.SetSpeed(50); // à 50% 
        Thread.Sleep(1000);
        thrust.SetSpeed(50); // à 10% 
        Thread.Sleep(5000);
        thrust.SetSpeed(60); // à 10% 
        Thread.Sleep(5000);
        thrust.SetSpeed(10); // à 10% 
        Thread.Sleep(5000);
        thrust.Stop();
        
        
        Console.ReadLine();
       
    }
}