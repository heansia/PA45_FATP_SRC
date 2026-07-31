using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.DUT.PA45
{
    public static class PrinterCommands
    {
        public static string functest(SUB_COMMAND cmd)
        {
            string m_Cmd = "";
            var field = cmd.GetType().GetField(cmd.ToString());
            if (field == null)
            {
                m_Cmd = cmd.ToString();
                goto End;
            }

            var descAttr = field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

            if (descAttr == null || descAttr.Description == null)
            {
                m_Cmd = cmd.ToString();
                goto End;
            }

            m_Cmd = descAttr.Description;
        End:

            return "run \"functest " + m_Cmd + "\"";
        }

        public const string LoginRoot = "run \"su -p pass itadmin\"";

        public const string GetFWVersion = "run \"cat /etc/version\"";

        public static string GetUbootVersion()
        {
            uname_option _u_option = uname_option.b;
            return _u_option.PrintSystemInformation();
        }

        public const string HEAD_LIFT_SENSOR = "run \"functest HEAD_LIFT_SENSOR\"";

        public static string WriteRTC(string currentTime)
        {
            return "run \"functest RTC " + currentTime + "\"";
        }

        public const string get_rtc = "run \"functest RTC\"";

        public const string TPH_ID = "run \"functest TPH_ID\"";

        public const string PRINTHEAD = "run \"functest PRINTHEAD\"";

        public const string BM = "run \"functest BM\"";

        public const string FRONT_BM = "run \"functest FRONT_BM\"";

        public const string LSS = "run \"functest LSS\"";

        public const string KEYPAD = "run \"functest KEYPAD\"";

        public const string AUDIO = "run \"functest AUDIO\"";

        public const string USB_HOST = "run \"functest USB HOST\"";
        
        public const string FORMFEED = "run \"functest FORMFEED\"";

        public const string testfeed_1 = "testfeed -1";

        public const string GET_MEDIA_LENGTH = "SETUP GET \"Printing,Media,Print Area,Media Length\"" + ",A$:?A$";

        public const string SetUpSleepTimer_0 = "SETUP \"System Settings,General,Power,Sleep Timer,0\"";

        public const string get_sleep_timer_value = "SETUP GET \"System Settings,General,Power,Sleep Timer\"" + ",A$:?A$";

        public const string get_media_type = "SETUP GET \"Printing,Media,Media Type\"" + ",A$";

        public const string get_media_type_value = "PRINT A$";

        public const string MOTOR_CLEANFEED_2000 = "run \"functest MOTOR CLEANFEED 2000\"";

        public const string BATTERY_TEST = "run \"functest BATTERY_TEST\"";

        public static string SetBtName(string btName)
        {
            return "run \"functest SET_BTNAME " + btName + "\"";
        }

        public const string BLUETOOTH_ID = "run \"functest BLUETOOTH_ID\"";

        public const string WIFI_ID = "run \"functest WIFI_ID\"";

        public const string ifconfig_a = "run \"ifconfig -a\"";

        public const string LTS = "run " + "\"functest LTS\"";

        public const string Quit = "q";

        public const string modules_printer = "run \"modules printer -p phuture -d\"";

        public static string set_modules_cpu(string pcba_PN, string pcba_SN, string mech)
        {
            return "run \"modules cpu -p phuture -f -m " + pcba_PN + " -s " + pcba_SN + " -o Mech" + "[" + mech + "]" + "\"";
        }

        public const string modules_cpu = "run \"modules cpu -p phuture -d\"";

        public static string set_modules_printer(string printer_PN, string printer_SN, string region)
        {
            return "run \"modules printer -p phuture -f -m " + printer_PN + " -s " + printer_SN + " -o Region" + "[" + region + "]" + "\"";
        }

        public static string set_modules_printer_bt_wifi(string printer_PN, string printer_SN, string region)
        {
            //run "modules printer -p phuture -f -m RP2F0000D20 -s 24141B689D -o Radio['BT+WIFI'] Region['ROW']
            return "run \"modules printer -p phuture -f -m " + printer_PN + " -s " + printer_SN + " -o " + "Radio[BT+WIFI]" + " Region" + "[" + region + "]" + "\"";
        }

        public static string set_modules_printer_bt(string printer_PN, string printer_SN, string region, string mac)
        {
            //run "modules printer -p phuture -f -m RP2F0000D20 -s 24141B689D -o Radio['BT'] Region['ROW'] btmac[10:20:30:40:50:60]
            return "run \"modules printer -p phuture -f -m " + printer_PN + " -s " + printer_SN + " -o " + "Radio[BT]" + " Region" + "[" + region + "]" + " btmac[" + mac + "]" + "\"";
        }

        public const string LED_CHARGING_ORANGE = "run \"functest LED CHARGING_ORANGE\"";

        public const string LED_ORANGE_ON = "run \"functest LED ALL 1\"";

        public const string LED_GREEN_ON = "run \"functest LED ALL 0\"";

        public const string MOTION = "run \"functest MOTION\"";

        public const string CHARGER = "run \"functest CHARGER\"";

        public const string LCD_GREEN = "run \"functest LCD GREEN\"";

        public const string LCD_BLACK = "run \"functest LCD BLACK\"";

        public const string LCD_WHITE = "run \"functest LCD WHITE\"";

        public const string get_printer_sensor_status = "run \"cat /proc/printer/sensor\"";

        public const string username = "root";

        public const string password = "f!j!r0cks";

        public const string RESTORE_DEFAULT = "run \"functest RESTORE_DEFAULT\"";

        public const string reboot = "run \"reboot\"";

        public const string chekc_locking_status = "run \"cat /proc/cmdline\"";

        public const string recovery_network_config = "run \"recovery.sh network config systemfiles userfiles\"";

        public const string SECURITY_CHIP_OTP = "run \"functest SECURITY_CHIP OTP\"";

        public const string SECURITY_CHIP_GEN_KEY = "run \"functest SECURITY_CHIP GEN_KEY\"";

        public const string SECURITY_CHIP_GET_DEVICE_ID = "run \"functest SECURITY_CHIP GET_DEVICE_ID\"";

        public const string SELFSTRIP = "run \"functest SELFSTRIP\"";

        public const string read_bluetooth_mac_for_bt_only_module = "run \"modules printer -p phuture -dhclient\"";

        public static string write_bluetooth_mac_for_bt_only_module(string mac)
        {
            return "run \"modules printer -p phuture -f -m " + "PA4512345678" + " -s " + "202606010005" + " -o Radio[BT] Region[] btmac" + "[" + mac + "]" + "\"";
        }

        public const string get_ip_addr_wifi = "run \"functest GETIPADDR_WIFI\"";

        public static string ping_ip_adderss(string ip)
        {
            return "run \"ping " + ip + " -w 5\"" + Environment.NewLine;
        }

        public static string testlabel_cmd(testlabel_list label)
        {
            return "run \"testlabel " + label + "\"";
        }
    }

    public enum SUB_COMMAND
    {
        REPORT,
        MLB_ID,
        DUART_ID,
        IEEE1284_ID,
        INDUSTRIAL_ID,
        APPLICATOR_ID,
        RFID_ID,
        UI_MODULE_ID,
        TOUCHLESS_ID,
        PSU_ID,
        CUTTER_ID,
        TPH_ID,
        BLUETOOTH_ID,
        WIFI_ID,
        ETHERNET_ID,
        BATTERY_ID,
        BATTERYBASE_ID,
        HEAD_LIFT_SENSOR,
        TT_MODULE,
        BM,
        LSS,
        FRONT_BM,
        FRONT_LSS,
        LTS,
        MLS,
        RLS,
        PRINTHEAD,
        SELFSTRIP,
        FORMFEED,
        CUTTER,
        OTP,
        BT,
        MOTORCURRENT,
        DUART,
        IEEE1284,
        INDUSTRIAL,
        RFID,

        [arguments("HOST")]
        USB,
        BATTERY_TEST,
        AUDIO,
        LED,
        KEYPAD,
        LCD,
        TOUCHSCREEN,
        TOUCHLESS,
        SYSTEM_NAME,
        COMMAND_LANGUAGE,
        SET_BTNAME,
        MEDIA_WIDTH,
        MEDIA_LENGTH,
        PRINT_SPEED,
        IP_ASSIGNMENT,
        IP_ADDRESS,
        NETMASK,
        ETH_GATEWAY,
        GETIPADDR_ETH,
        GETIPADDR_WIFI,
        WIFI_SETUP,
        RTC,
        RTC_BATTERY,
        VERIFIER,
        FONTS,
        SELF_TEST,
        PSU_TEMP,
        GPIO,
        MOTOR,
        RES,
        FRONT_ARM_LIFT,
        LICENSES,
        RESTORE_DEFAULT,
        APPLICATOR,
        REBOOT,
        RED_REV,
        UART,
        MFI,

        [Description("RFID CALIBRATION")]
        RFID_CALIBRATION,

        [Description("RFID READDATA")]
        RFID_READDATA,

        [Description("SET_RFID_TAG_TYPE ONLYREAD")]
        SET_RFID_TAG_TYPE_ONLYREAD,

        [Description("SET_RFID_TAG_TYPE READWRITE")]
        SET_RFID_TAG_TYPE_READWRITE
    }


    

}
