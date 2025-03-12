using RpLidar.NET;
using RpLidar.NET.Entities;

namespace VoitureAutonome;

public class AutoDriveV8
{
    public bool IsRunning = false;
    
    private RPLidar lidar;
    
    int[] distance = new int[180];
    
    public AutoDriveV8()
    {
       
    }

    private void Lidar_LidarPointScanEvent(List<LidarPoint> points)
    {
        distance = new int[180];
        foreach (var point in points)
        {
            distance[(int)point.Angle] = (int)point.Distance;
        }

        PrintAllPoints();
    }

    private void PrintAllPoints()
    {
        Console.WriteLine("--------------------");
        for (int i = 0; i < 180; i++)
        {
            Console.WriteLine( i + " -> " + distance[i]);
        }
        Console.WriteLine("--------------------");
    }

    public void Run()
    {
        IsRunning = true;
        
        lidar = new RPLidar("/dev/ttyUSB0", 256000);
        lidar.LidarPointScanEvent += Lidar_LidarPointScanEvent;
        

        while (IsRunning)
        {
            Thread.Sleep(1000);
            PrintAllPoints();

        }
        lidar.Stop();
        lidar.Dispose();
    }

    public void Stop()
    {
        IsRunning = false;
    }
    
    
}