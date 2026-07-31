using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.DUT.QBC.I2C
{
    public static class I2C
    {
        public static string CurrentInitTestCmd(I2C_Current currId)
        {
            //0x57, 2, BQIC1, 1
            string command = string.Format($"0x57, 2, {currId}, 1");

            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }
            return command;
        }

        public static string BatteryTemperatureCmd(I2C_Battery batteryId)
        {
            //0x57, 2, BAT_1, 1

            string command = string.Format($"0x57, 2, {batteryId}, 1");
            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }
            return command;
        }

        public static string BatteryCapacityCmd(I2C_Battery batteryId)
        {
            //0x57, 2, BAT_1, 2
            string command = string.Format($"0x57, 2, {batteryId}, 2");

            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }

            return command;
        }

        public static string BatteryCapVoltageCmd(I2C_Battery batteryId)
        {
            //0x57, 2, BAT_1, 3
            string command = string.Format($"0x57, 2, {batteryId}, 3");

            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }
            return command;
        }

    }
}
