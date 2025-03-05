using System.IO.Ports;

namespace VoitureAutonome;

public class RPLidar
{
    private const byte SYNC_BYTE = 0xA5;
    private const byte SYNC_BYTE2 = 0x5A;
    private const byte GET_INFO_BYTE = 0x50;
    private const byte GET_HEALTH_BYTE = 0x52;
    private const byte STOP_BYTE = 0x25;
    private const byte RESET_BYTE = 0x40;
    private const byte SET_PWM_BYTE = 0xF0;

    private const int DESCRIPTOR_LEN = 7;
    private const int INFO_LEN = 20;
    private const int HEALTH_LEN = 3;

    private const int INFO_TYPE = 4;
    private const int HEALTH_TYPE = 6;

    private const int MAX_MOTOR_PWM = 1023;
    private const int DEFAULT_MOTOR_PWM = 660;
    private ExpressPacket _expressData;
    private int _expressTrame = 32;

    private readonly Dictionary<int, string> _healthStatuses = new()
    {
        { 0, "Good" },
        { 1, "Warning" },
        { 2, "Error" }
    };

    private bool _motorRunning;
    private readonly int _motorSpeed = 1023;
    private bool _scanning;
    private readonly SerialPort _serialPort;

    public RPLidar(string port, int baudrate = 115200, int timeout = 1)
    {
        _serialPort = new SerialPort(port, baudrate)
        {
            Parity = Parity.None,
            StopBits = StopBits.One,
            ReadTimeout = timeout,
            WriteTimeout = timeout
        };
    }


    public void Connect()
    {
        if (_serialPort.IsOpen) Disconnect();
        try
        {
            _serialPort.Open();
        }
        catch (Exception ex)
        {
            throw new RPLidarException($"Failed to connect to the sensor due to: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        if (_serialPort.IsOpen) _serialPort.Close();
    }

    public void StartMotor()
    {
        Console.WriteLine("Starting motor");
        _serialPort.DtrEnable = false;
        SetPwm(_motorSpeed);
        _motorRunning = true;
    }

    public void StopMotor()
    {
        Console.WriteLine("Stopping motor");
        SetPwm(0);
        Thread.Sleep(1);
        _serialPort.DtrEnable = true;
        _motorRunning = false;
    }

    private void SetPwm(int pwm)
    {
        var payload = BitConverter.GetBytes((ushort)pwm);
        SendPayloadCmd(SET_PWM_BYTE, payload);
    }

    private void SendPayloadCmd(byte cmd, byte[] payload)
    {
        byte[] size = { (byte)payload.Length };
        var req = new[] { SYNC_BYTE, cmd }.Concat(size).Concat(payload).ToArray();
        byte checksum = 0;
        foreach (var b in req) checksum ^= b;
        req = req.Concat(new[] { checksum }).ToArray();
        _serialPort.Write(req, 0, req.Length);
    }

    private void SendCmd(byte cmd)
    {
        byte[] req = { SYNC_BYTE, cmd };
        _serialPort.Write(req, 0, req.Length);
    }

    private (int, bool, int) ReadDescriptor()
    {
        var descriptor = new byte[DESCRIPTOR_LEN];
        _serialPort.Read(descriptor, 0, DESCRIPTOR_LEN);
        if (descriptor.Length != DESCRIPTOR_LEN || descriptor[0] != SYNC_BYTE || descriptor[1] != SYNC_BYTE2)
            throw new RPLidarException("Incorrect descriptor");
        var isSingle = descriptor[5] == 0;
        return (descriptor[2], isSingle, descriptor[6]);
    }

    private byte[] ReadResponse(int dsize)
    {
        while (_serialPort.BytesToRead < dsize) Thread.Sleep(1);
        var data = new byte[dsize];
        _serialPort.Read(data, 0, dsize);
        return data;
    }

    public Dictionary<string, object> GetInfo()
    {
        if (_serialPort.BytesToRead > 0) return null;
        // throw new RPLidarException("Data in buffer, you can't have info! Run CleanInput() to empty the buffer.");
        SendCmd(GET_INFO_BYTE);
        var (dsize, isSingle, dtype) = ReadDescriptor();
        if (dsize != INFO_LEN || !isSingle || dtype != INFO_TYPE) throw new RPLidarException("Wrong get_info reply");
        var raw = ReadResponse(dsize);
        var serialNumber = BitConverter.ToString(raw, 4, 16).Replace("-", "");
        return new Dictionary<string, object>
        {
            { "model", raw[0] },
            { "firmware", (raw[2], raw[1]) },
            { "hardware", raw[3] },
            { "serialnumber", serialNumber }
        };
    }


    private void ClearSerialBuffer()
    {
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
    }

    public (string, int) GetHealth(int maxRetries = 3)
    {
        for (var attempt = 0; attempt < maxRetries; attempt++)
            try
            {
                ClearSerialBuffer();
                SendCmd(GET_HEALTH_BYTE);

                var (dsize, isSingle, dtype) = ReadDescriptor();
                if (dsize != HEALTH_LEN || !isSingle || dtype != HEALTH_TYPE)
                    throw new RPLidarException("Wrong get_health reply");

                var raw = ReadResponse(dsize);
                if (raw.Length < HEALTH_LEN) throw new RPLidarException("Incomplete health data");

                var status = _healthStatuses.ContainsKey(raw[0]) ? _healthStatuses[raw[0]] : "Unknown";
                var errorCode = (raw[1] << 8) + raw[2];
                return (status, errorCode);
            }
            catch (RPLidarException ex)
            {
                if (attempt == maxRetries - 1) throw; // Relancer l'exception après le dernier essai
                Thread.Sleep(50); // Attendre avant de réessayer
            }

        throw new RPLidarException("Failed to get health status after multiple attempts");
    }

    public void CleanInput()
    {
        if (_scanning) throw new RPLidarException("Cleaning not allowed during scanning process active!");
        _serialPort.DiscardInBuffer();
        _expressTrame = 32;
        _expressData = null;
    }

    public void Stop()
    {
        SendCmd(STOP_BYTE);
        Thread.Sleep(100);
        _scanning = false;
        CleanInput();
    }

    public void Start(string scanType = "normal")
    {
        if (_scanning) throw new RPLidarException("Scanning already running!");

        // Vérifier l'état de santé du LIDAR
        (var status, var errorCode) = GetHealth();

        if (status == "Error")
        {
            // En cas d'erreur, réinitialiser le LIDAR
            Reset();
            (status, errorCode) = GetHealth();

            if (status == "Error") throw new RPLidarException($"RPLidar hardware failure. Error code: {errorCode}");
        }
        else if (status == "Warning")
        {
            Console.WriteLine($"Warning sensor status detected! Error code: {errorCode}");
        }

        // Envoyer la commande de démarrage du scan
        var cmd = GetScanTypeByte(scanType);
        if (scanType == "express")
            SendPayloadCmd(cmd, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 });
        else
            SendCmd(cmd);

        // Lire le descripteur de réponse
        var (dsize, isSingle, dtype) = ReadDescriptor();

        // Vérifier la validité de la réponse
        if (dsize != GetScanTypeSize(scanType) || isSingle || dtype != GetScanTypeResponse(scanType))
        {
            CleanInput();
            throw new RPLidarException("Wrong scan reply");
        }

        _scanning = true;
    }

    public void Reset()
    {
        SendCmd(RESET_BYTE);
        Thread.Sleep(2000);
        CleanInput();
    }

    public IEnumerable<(bool, int, float, float)> IterMeasures(string scanType = "normal", int maxBufMeas = 3000)
    {
        StartMotor();
        if (!_scanning) Start(scanType);
        while (true)
        {
            var dsize = GetScanTypeSize(scanType);
            if (maxBufMeas > 0 && _serialPort.BytesToRead > maxBufMeas)
            {
                Stop();
                Start(scanType);
            }

            if (scanType == "normal")
            {
                var raw = ReadResponse(dsize);
                yield return ProcessScan(raw);
            }
            else if (scanType == "express")
            {
                if (_expressTrame == 32)
                {
                    _expressTrame = 0;
                    if (_expressData == null) _expressData = ExpressPacket.FromBytes(ReadResponse(dsize));
                    _expressData = ExpressPacket.FromBytes(ReadResponse(dsize));
                }

                _expressTrame++;
                yield return ProcessExpressScan(_expressData, _expressTrame);
            }
        }
    }

    private (bool, int, float, float) ProcessScan(byte[] raw)
    {
        var newScan = (raw[0] & 0b1) == 1;
        var inversedNewScan = ((raw[0] >> 1) & 0b1) == 1;
        if (newScan == inversedNewScan) throw new RPLidarException("New scan flags mismatch");
        var quality = raw[0] >> 2;
        if ((raw[1] & 0b1) != 1) throw new RPLidarException("Check bit not equal to 1");
        var angle = ((raw[1] >> 1) + (raw[2] << 7)) / 64.0f;
        var distance = (raw[3] + (raw[4] << 8)) / 4.0f;
        return (newScan, quality, angle, distance);
    }

    private (bool, int, float, float) ProcessExpressScan(ExpressPacket data, int trame)
    {
        // Vérifie si c'est un nouveau scan en fonction de l'angle de départ et de l'angle actuel
        var newScan = trame == 1 && data.StartAngle < data.Angle[trame - 1];

        // Calcule l'angle en fonction de la trame actuelle
        var angle = (data.StartAngle + (data.Angle[trame - 1] - data.StartAngle) % 360 / 32 * trame) % 360;

        // Récupère la distance correspondant à la trame actuelle
        var distance = data.Distance[trame - 1];

        // Retourne les résultats
        return (newScan, 0, angle, distance);
    }

    private byte GetScanTypeByte(string scanType)
    {
        switch (scanType)
        {
            case "normal": return 0x20;
            case "force": return 0x21;
            case "express": return 0x82;
            default: throw new ArgumentException("Invalid scan type");
        }
    }

    private int GetScanTypeSize(string scanType)
    {
        switch (scanType)
        {
            case "normal": return 5;
            case "force": return 5;
            case "express": return 84;
            default: throw new ArgumentException("Invalid scan type");
        }
    }

    private int GetScanTypeResponse(string scanType)
    {
        switch (scanType)
        {
            case "normal": return 129;
            case "force": return 129;
            case "express": return 130;
            default: throw new ArgumentException("Invalid scan type");
        }
    }
}

public class ExpressPacket
{
    public ExpressPacket(float[] distance, float[] angle, bool newScan, float startAngle)
    {
        Distance = distance;
        Angle = angle;
        NewScan = newScan;
        StartAngle = startAngle;
    }

    public float[] Distance { get; }
    public float[] Angle { get; }
    public bool NewScan { get; }
    public float StartAngle { get; }

    public static ExpressPacket FromBytes(byte[] data)
    {
        if (data[0] >> 4 != 0xA || data[1] >> 4 != 0x5) throw new ArgumentException("Invalid express packet");

        byte checksum = 0;
        for (var i = 2; i < data.Length; i++) checksum ^= data[i];
        if (checksum != (data[0] & 0x0F) + ((data[1] & 0x0F) << 4)) throw new ArgumentException("Invalid checksum");

        var newScan = data[3] >> 7 == 1;
        var startAngle = (data[2] + ((data[3] & 0x7F) << 8)) / 64.0f;

        var distance = new float[16];
        var angle = new float[16];
        for (var i = 0; i < 80; i += 5)
        {
            distance[i / 5] = ((data[i + 4] >> 2) + (data[i + 5] << 6)) / 4.0f;
            angle[i / 5] = ((data[i + 8] & 0x0F) + ((data[i + 4] & 0x01) << 4)) / 8.0f *
                           ((data[i + 4] & 0x02) == 0 ? 1 : -1);
            distance[i / 5 + 1] = ((data[i + 6] >> 2) + (data[i + 7] << 6)) / 4.0f;
            angle[i / 5 + 1] = ((data[i + 8] >> 4) + ((data[i + 6] & 0x01) << 4)) / 8.0f *
                               ((data[i + 6] & 0x02) == 0 ? 1 : -1);
        }

        return new ExpressPacket(distance, angle, newScan, startAngle);
    }
}

public class RPLidarException : Exception
{
    public RPLidarException(string message) : base(message)
    {
    }
}