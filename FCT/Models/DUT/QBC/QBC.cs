using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.DUT.Interface;
using FCT.Models.DUT.QBC.Battery;
using FCT.Models.Products.Interface;
using INI = FCT.Util.INI_Parser;

namespace FCT.Models.DUT.QBC
{
    class QBC : DUT
    {
        public QBC(IProduct product) : base(product)
        {
            Product = product;
        }

        public override void Test()
        {
            if (TestMethod != null)
            {

            }
        }

        public static string GetFWVersionCmd()
        {
            string command = string.Format($"0x57, 0, VER");

            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }
            return command;
        }

        public static string EnterFactoryModeCmd()
        {
            /*
            string command = string.Format($"factory");

            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }
            return command;
            */

            return "factory";
        }

        public static string ExitFactoryModeCmd()
        {
            /*
            string command = string.Format($"factory");

            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }
            return command;
            */

            return "exitfactory         ";
        }

        public static string FCTBatteryStart()
        {
            string command = string.Format($"0x57, 4, BAT_START");

            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }
            return command;
        }

        public static string FCTBatteryEnd()
        {
            string command = string.Format($"0x57, 4, BAT_END");

            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }
            return command;
        }

        public static string GetPATestPointDI(BatteryBusID batteryBusID)
        {
            string command = string.Format($"0x57, 4, {batteryBusID}");

            int len = 20 - command.Length;
            if (len > 0) { for (int i = 0; i < len; i++) command = command + " "; }
            return command;
        }

        public override string VID { get { return string.Empty; } }

        public override string PID { get { return string.Empty; } }

    }
}
