using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.DUT.QBC.NTC
{
    class NTC
    {
        public static string ReadDeviceTempCmd(NTC_ID ntcId)
        {
            string command = string.Format($"0x57, 3, {ntcId}, 1");
            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }

            return command;
        }
    }
}
