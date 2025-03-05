using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Linq;

namespace VoitureAutonome;

public class RPLidarScanner
{
    private readonly RPLidar _lidar;
    private readonly ConcurrentDictionary<int, float> _scanData;
    private Thread _scanThread;
    private bool _running;

    public RPLidarScanner(string port, int baudrate = 256000)
    {
        _lidar = new RPLidar(port, baudrate);
        _scanData = new ConcurrentDictionary<int, float>();
    }

    public void StartScanning()
    {
        if (_running) return;
        
        _lidar.Connect();
        _lidar.StartMotor();

        _scanThread = new Thread(() =>
        {
            try
            {
                _lidar.Start("normal");
                _running = true;

                foreach (var (newScan, _, angle, distance) in _lidar.IterMeasures("normal"))
                {
                    if (!_running) break;
                    
                    int roundedAngle = (int)Math.Round(angle) % 360;
                    _scanData[roundedAngle] = distance;  // Mise à jour continue

                    if (newScan)
                    {
                        // Nettoyage des anciens points non mis à jour
                        foreach (var key in _scanData.Keys.Except(Enumerable.Range(0, 360)))
                            _scanData.TryRemove(key, out _);
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
        return new Dictionary<int, float>(_scanData); // Copie pour éviter les conflits d'accès
    }

    public void StopScanning()
    {
        _running = false;
        _lidar.Stop();
        _lidar.StopMotor();
        _lidar.Disconnect();
    }
}
