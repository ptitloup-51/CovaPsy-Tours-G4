using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace VoitureAutonome
{
    public class RPLidarScanner
    {
        private readonly RPLidar _lidar;
        private readonly ConcurrentDictionary<int, (float Distance, long Timestamp)> _scanData;
        private Thread _scanThread;
        private bool _running;
        private Stopwatch _stopwatch;

        public RPLidarScanner(string port, int baudrate = 256000)
        {
            _lidar = new RPLidar(port, baudrate);
            _scanData = new ConcurrentDictionary<int, (float, long)>();
            _stopwatch = new Stopwatch();
        }

        public void StartScanning()
        {
            if (_running) return;

            _lidar.Connect();
            _lidar.StartMotor();
            _stopwatch.Start();

            _scanThread = new Thread(() =>
            {
                try
                {
                    _lidar.Start("normal");
                    _running = true;

                    foreach (var (newScan, _, angle, distance) in _lidar.IterMeasures("normal"))
                    {
                        if (!_running) break;

                        int transformedAngle = (int)Math.Round((angle + 90) % 360);

                        if (transformedAngle >= 0 && transformedAngle <= 180)
                        {
                            long currentTime = _stopwatch.ElapsedMilliseconds;
                            if (_scanData.TryGetValue(transformedAngle, out var oldData))
                            {
                                long timeDiff = currentTime - oldData.Timestamp;
                                Console.WriteLine($"Angle {transformedAngle}: {timeDiff} ms");
                            }
                            _scanData[transformedAngle] = (distance, currentTime);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur du LIDAR : {ex.Message}");
                }
            })
            {
                IsBackground = true
            };

            _scanThread.Start();
        }

        public Dictionary<int, float> GetLatestScan()
        {
            return _scanData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Distance);
        }

        public void StopScanning()
        {
            _running = false;
            _lidar.Stop();
            _lidar.StopMotor();
            _lidar.Disconnect();
            _stopwatch.Stop();
        }
    }
}