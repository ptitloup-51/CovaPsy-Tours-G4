using System.Collections;

namespace VoitureAutonome;

public class Program
{
    public static void Main(string[] args)
    { 
        Console.WriteLine("Hello World!");
      //  RemoteDebug debug = new();

      
      #region lidar

      
      var lidar = new RPLidar("/dev/ttyUSB0", 256000);
      lidar.Connect();
      lidar.StopMotor();
      Console.WriteLine("arret du lidar");
      Thread.Sleep(3000);
      Console.WriteLine("rotation du lidar");

      var info = lidar.GetInfo();
      Console.WriteLine($"Model: {info["model"]}, Serial: {info["serialnumber"]}");

      var health = lidar.GetHealth();
      Console.WriteLine($"Health: {health.Item1}, Error Code: {health.Item2}");

      lidar.CleanInput();
      lidar.StartMotor();
     // lidar.Start();
      
     
        /*
      foreach (var measure in lidar.IterMeasures())
      {
          Console.WriteLine($"New Scan: {measure.Item1}, Quality: {measure.Item2}, Angle: {measure.Item3}, Distance: {measure.Item4}");
      }
      */
        
       

        var measure = lidar.IterMeasures("normal");

        foreach (var mes in measure)
        {
            double angle = mes.Item3; // Angle de la mesure
            if (angle >= -2 && angle <= 2)
            {
                Console.WriteLine($"Measure: {mes.Item1}, Quality: {mes.Item2}, Angle: {mes.Item3}, Distance: {mes.Item4}");
            }
        }

      lidar.StopMotor();
      lidar.Disconnect();

      #endregion
      
      
      /*
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
      
      */
      
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