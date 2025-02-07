namespace VoitureAutonome;

public class Program
{
    public static void Main(string[] args)
    { 
      //  Console.WriteLine("Hello World!");
      //  RemoteDebug debug = new();
      

      
      Steering steering = new Steering();
      Thrust thrust = new Thrust();
      
      steering.SetDirection(30);
      Thread.Sleep(100);
      
      steering.Center();
      thrust.SetSpeed(5);
      
      Thread.Sleep(1000);
      steering.SetDirection(70);
      Thread.Sleep(5000);
      
      
      thrust.Dispose();
      steering.Dispose();         // Arrête et libère les ressources
      
      
      
      /*
        Thrust thrust = new Thrust();
        thrust.SetSpeed(10); // à 50% 
        Thread.Sleep(4000);
        thrust.SetSpeed(40); // à 50% 
        Thread.Sleep(1000);
        thrust.Dispose();
        */
        
        // Lidar lid = new();
        
      //Direction dir = new Direction();
     // dir.SetDirectionDegre(10);
    //  Thread.Sleep(5000);
    //  Direction.Dispose();
        
       /*
        Thrust thrust = new Thrust();
        thrust.SetSpeed(10); // à 50% 
        Thread.Sleep(2000);
        thrust.SetSpeed(0);
        Thread.Sleep(1000);
        thrust.SetSpeed(10); // à -10% 
        Thread.Sleep(2000);
        thrust.Dispose(); //supprime l'objet
        */
        
        
        Console.ReadLine();
        
    }
}