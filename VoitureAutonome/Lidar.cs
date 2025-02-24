using System;
using System.Threading;
using RPLidar;


namespace VoitureAutonome
{
    class Test
    {
        private readonly Lidar lidar = new Lidar();
        public Test()
        {
          lidar.PortName = "/dev/ttyUSB0";
          lidar.Baudrate = 256000;
          lidar.GetHealth();
        }
    }
}