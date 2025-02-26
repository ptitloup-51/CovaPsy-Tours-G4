using RPLidar4Net.Api.Data;
using RPLidar4Net.IO;

namespace VoitureAutonome
{
    public class Test
    {
        static RPLidarSerialDevice _rpLidar;
        
        
        public Test()
        {
            _rpLidar = new RPLidarSerialDevice("/dev/ttyUSB0", 256000);
            //Connect RPLidar
            _rpLidar.Connect();
            Thread.Sleep(10);
            Console.WriteLine(_rpLidar.GetInfo().ModelID);
            //Reset - Not really sure how this is supposed to work, reconnecting USB works too
            _rpLidar.Reset();
            Thread.Sleep(1000);
            
            //Stop motor
            _rpLidar.StopMotor();
            //Get Device Information
            InfoDataResponse infoDataResponse = _rpLidar.GetInfo();
            Console.WriteLine(infoDataResponse.FirmwareVersion);
            Console.WriteLine(infoDataResponse.SerialNumber);
            //Get Device Health
            HealthDataResponse healthDataResponse = _rpLidar.GetHealth();

            
            Console.WriteLine(_rpLidar.GetTypicalScanMode());
           Console.WriteLine(_rpLidar.GetScanModeName(3));

           _rpLidar.NewScan += RPLidar_NewScan;

           Console.WriteLine(_rpLidar.GetHealth().Status);
           try
           {
               _rpLidar.MotorSpeed = 10;
               _rpLidar.StartMotor();
               _rpLidar.StartScan();
               
               Thread.Sleep(1000);
               
               Console.WriteLine(_rpLidar.GetHealth().Status);
           }
           catch (Exception e)
           {
               Console.WriteLine(e);
           }
           
           Thread.Sleep(2000);
        
         

        }
        
        private void RPLidar_NewScan(object sender, NewScanEventArgs eventArgs)
        {
           Console.WriteLine("New scan");
        }



    }

}
