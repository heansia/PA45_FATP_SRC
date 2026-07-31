using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Instrument.Interface;

namespace FCT.Models.Instrument
{
    class KEITHLEY_2306_LAN : IInstrument
    {
        public event EventHandler<RunTimeLogUpdatedEventArgs> UpdateStatus;

        private readonly TcpClient _tcpClient = new TcpClient();
        private NetworkStream _networkStream;

        public string MAC_Address = "FE:34:57:12:57:12";

        public int Port { get; set; }

        public string _subnetMask = "255.255.255.0";
        public string SubnetMask
        {
            get { return _subnetMask; }

            set
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)) { return; }

                _subnetMask = value;
            }
        }

        public string _gateway = "192.168.000.001";
        public string Gateway
        {
            get { return _gateway; }

            set
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)) { return; }

                _gateway = value;
            }
        }

        public string IP_Address { get; set; }

        public KEITHLEY_2306_LAN(string ipAddress, int port = 5025)
        {
            IP_Address = ipAddress;
            Port = port;
        }

        private bool _isConnected = false;
        public bool IsConnected { get { return _isConnected; } }

        public void on_runtime_log_update(string msg)
        {
            Console.WriteLine(msg);
            if (UpdateStatus != null) { UpdateStatus.Invoke(this, new RunTimeLogUpdatedEventArgs(msg)); }
        }

        public void Connect()
        {
            try
            {
                on_runtime_log_update("pc is connecting to KEITHLEY 2306-LAN...");
                _tcpClient.Connect(IP_Address, Port);
                _tcpClient.ReceiveTimeout = 3000; // 设置读取超时 5秒
                _tcpClient.SendTimeout = 3000;
                _networkStream = _tcpClient.GetStream();
                _isConnected = true;
                on_runtime_log_update("pc is connected to KEITHLEY 2306-LAN...");
            }
            catch (Exception e)
            {
                on_runtime_log_update($"error happened {e.Message.ToString()} while connecting to KEITHLEY 2306-LAN...");
                _isConnected = false;
            }
        }

        public void IsExist()
        {

        }

        public void Source(double voltage, int channelIndex)
        {

        }

        public string GetDeviceName()
        {
            if (!_isConnected)
            {
                return "No_Device";
            }

            // 2. 发送查询指令 (*IDN?)
            string command = "*IDN?\n"; // 注意：SCPI指令通常必须以 \n (换行符) 结尾
            byte[] sendData = Encoding.ASCII.GetBytes(command);
            _networkStream.Write(sendData, 0, sendData.Length);

            // 3. 读取返回值
            byte[] buffer = new byte[256];
            int bytesRead = _networkStream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
        }

        public void Measure(MeasureType type, int channelIndex)
        {
            if (type == MeasureType.Current)
            {

            }
            if (type == MeasureType.Voltage)
            {

            }
        }

        // 封装一个发送指令的辅助方法（不需要返回值的指令）
        static void SendCommand(NetworkStream stream, string command)
        {
            byte[] data = Encoding.ASCII.GetBytes(command + "\n");
            stream.Write(data, 0, data.Length);
            stream.Flush();
            System.Threading.Thread.Sleep(100); // 稍微延时，防止指令发送过快导致仪器缓冲区溢出

        }

        public bool Open()
        {
            if (!_isConnected) { Connect(); }

            return _isConnected;
        }

        public bool Close()
        {
            throw new NotImplementedException();
        }
    }

    enum MeasureType
    {
        Voltage,
        Current
    }
}
