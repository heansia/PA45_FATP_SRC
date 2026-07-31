using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.DUT.QBC.LED
{
    class LED
    {
        public static string LedTestCommand(LED_ID ledId, bool bStatus)
        {
            int i_flag = bStatus ? 1 : 2;
            string command = string.Format($"0x57, 1, {ledId}, {i_flag}");

            int len = 20 - command.Length;
            if (len > 0)
            {
                for (int i = 0; i < len; i++) command = command + " ";
            }

            return command;
        }
        
        public static string LedTestResult(LED_ID led, bool bStatus)
        {
            string _status = bStatus ? "on" : "off";
            return string.Format($"{led.ToString().Replace("L_", "")} ledpin{_status}");
        }


    }
}
