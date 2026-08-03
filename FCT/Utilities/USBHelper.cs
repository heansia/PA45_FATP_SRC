using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using FCT.Models;

namespace FCT.Utilities
{
    public static class USBHelper
    {
        [DllImport("USB_Printer.dll")]
        public static extern int EX_OpenUSBDevicePrint();

        [DllImport("USB_Printer.dll")]
        public static extern int EX_SetOutputReportPrint(byte[] byte_);

        [DllImport("USB_Printer.dll")]
        public static extern int EX_GetInputReportPrint(byte[] byte_, int cmd);

        [DllImport("USB_Printer.dll")]
        public static extern int EX_CloseUSBDevicePrint();

        public static event USBDataReceivedEventHanlder USBDataReceived;

        public static event USBDataReceivedEventHanlder USBDataSend;
        
        public static void SendCommand(string cmdContent)
        {
            byte[] data = new byte[64];
            data = System.Text.Encoding.Default.GetBytes(cmdContent);
            EX_SetOutputReportPrint(data);
            
        }

        public static void OnUSBDataSend()
        {
            if (USBDataSend != null)
            {
                //this.Invoke(new Action)
            }
        }

    }

    public delegate void USBDataReceivedEventHanlder(object sender, USBDataReceivedEventArgs e);

    public class USBDataReceivedEventArgs : EventArgs
    {
        public byte[] Buffer { get; }
        public string HexBuffer { get; }
        public string AsciiBuffer { get; }
        
        public USBDataReceivedEventArgs(byte[] buffer)
        {
            if (Buffer == null)
                Buffer = new byte[64];
        }
    }

    public delegate void USBDataSendEventHanlder(object sender, USBDataReceivedEventArgs e);

    public class USBDataSendEventArgs : EventArgs
    {
        public byte[] Buffer { get; }
        public string HexBuffer { get; }
        public string AsciiBuffer { get; }
        
        public USBDataSendEventArgs(byte[] buffer)
        {
            if (Buffer == null)
                Buffer = new byte[64];
        }
    }
}
