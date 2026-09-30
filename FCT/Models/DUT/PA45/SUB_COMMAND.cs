using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.DUT.PA45
{
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
