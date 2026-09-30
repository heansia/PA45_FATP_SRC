using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.DUT.PA45
{
    public static class PA45_FCT_Commands
    {
        public static string functest(SUB_COMMAND cmd)
        {
            return $"functest {cmd}";
        }

        public const string Username = "root";

        public const string Password = "pass";

        public const string root_INTERMEC = "root@INTERMEC_$123$ ~$";

        public const string root_OK_ConsoleTag = "root@INTERMEC_$123$ ~$";

        public const string ConsoleLoginReminder = "INTERMEC_$123$ login:";

        public const string none_login = "(none) login:";

        public const string none_root_ok = "root@(none) ~$";

        public const string GetSensorValue = "cat /proc/printer/sensor";

        public const string GetFWVersion = "cat /etc/version";

        public const string GetMCUFWVersion = "cat /proc/sys/printer/mcudev/fw_ver";

        public static string SetMemoryAddressValue(string address, string value)
        {
            return $"memtool -32 {address}={value}";
        }

        public const string motor_cleanfeed = "functest MOTOR CLEANFEED 500";

        public const string motor_cleanfeed_1 = "functest MOTOR CLEANFEED -500";

        public static string GetMemoryAddressValue(string address)
        {
            return $"memtool -32 {address} 1";
        }

        public static string Set_MCU_Pin_DO(int index, MCU_DO_Status status)
        {
            return $"echo {index} > /proc/sys/printer/mcudev/gpio_num{Environment.NewLine}echo {Convert.ToInt32(status)} > /proc/sys/printer/mcudev/gpio";
        }

        public static string Set_MCU_Pin_DI(int index, MCU_DO_Status status)
        {
            return $"echo {index} > /proc/sys/printer/mcudev/gpio_num{Environment.NewLine}echo {Convert.ToInt32(status)} > /proc/sys/printer/mcudev/gpio";
        }

        public static string Get_MCU_Pin_DI(int index)
        {
            return $"echo {index} > /proc/sys/printer/mcudev/gpio_num{Environment.NewLine}cat /proc/sys/printer/mcudev/gpio";
        }

        public static string Get_MCU_Pin_ADC(int index)
        {
            return $"echo {index} > /proc/sys/printer/mcudev/adc_read{Environment.NewLine}cat /proc/sys/printer/mcudev/adc_read";
        }

        public static string set_system_kernel_time()
        {
            return "date -s " + "\"" + $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
        }

        public const string write_hwclock_to_rtc = "hwclock -w -f /dev/rtc0";

        public const string clear_wakealarm = "echo +5 > /sys/class/rtc/rtc0/wakealarm";

        public const string set_wakealarm = "echo +5 > /sys/class/rtc/rtc0/wakealarm";

        public const string power_sleep = "echo mem > /sys/power/state";

        public const string Set_BM_High = "functest BM Drive 255";

        public const string Set_BM_Low = "functest BM Drive 0";

        public const string Set_Front_BM_High = "functest FRONT_BM Drive 255";

        public const string Set_Front_BM_Low = "functest FRONT_BM Drive 0";

        public const string Set_LSS_High = "functest LSS Drive 255";

        public const string Set_LSS_Low = "functest BM Drive 0";

        public const string Set_Front_LSS_High = "functest FRONT_BM Drive 255";

        public const string Set_Front_LSS_Low = "functest FRONT_BM Drive 0";

        public const string functest_motor_cleanfeed_1000 = "functest MOTOR CLEANFEED 1000";

        public const string functest_mfi_info = "functest MFI INFO";

        public const string functest_motion = "functest MOTION";

        public const string get_tph_res = "res 100";

        public const string unlock_memtool_cmd1 = "P=$(find /sys/devices -name '*20ac000.gpio*' | head -1)";

        public const string unlock_memtool_cmd2 = "echo on > $P/power/control";

        public const string ls_media = "ls /media";

        private static string _i2c_charger = "";

        public static string i2c_charger
        {
            get
            {
                return _i2c_charger;
            }
        }

        private static string _i2c_mfi_0x10 = "";

        public static string i2c_mfi_0x10
        {
            get
            {
                return _i2c_mfi_0x10;
            }
        }

        private static string _i2c_motion_0x18 = "";

        public static string i2c_motion_0x18
        {
            get
            {
                return _i2c_motion_0x18;
            }
        }

        private static string _i2c_rtc_0x00 = "";

        public static string i2c_rtc_0x00
        {
            get
            {
                return _i2c_rtc_0x00;
            }
        }

        private static string _i2c_rtc_0x51 = "";

        public static string i2c_rtc_0x51
        {
            get
            {
                return _i2c_rtc_0x51;
            }
        }

        private static string _provCmd_security_ic = "";

        public static string provCmd_security_ic
        {
            get
            {
                return _i2c_rtc_0x51;
            }
        }

        static PA45_FCT_Commands()
        {
            _i2c_charger = File.ReadAllText(Path.Combine(Global.ASSEMBLY_PATH, "FCT_Cmd", "i2c_charger_IC.txt"));
            _i2c_mfi_0x10 = File.ReadAllText(Path.Combine(Global.ASSEMBLY_PATH, "FCT_Cmd", "i2c_mfi_0x10.txt"));
            _i2c_motion_0x18 = File.ReadAllText(Path.Combine(Global.ASSEMBLY_PATH, "FCT_Cmd", "i2c_motion_0x18.txt"));
            _i2c_rtc_0x00 = File.ReadAllText(Path.Combine(Global.ASSEMBLY_PATH, "FCT_Cmd", "i2c_rtc_0x00.txt"));
            _i2c_rtc_0x51 = File.ReadAllText(Path.Combine(Global.ASSEMBLY_PATH, "FCT_Cmd", "i2c_rtc_0x51.txt"));
            _provCmd_security_ic = File.ReadAllText(Path.Combine(Global.ASSEMBLY_PATH, "FCT_Cmd", "provCmd_security_ic.txt"));
        }

    }

}
