using System;
using System.Collections.Generic;
using System.Linq;
using FCT.Models.DUT.Interface;
using FCT.Models.DUT.PA45;
using FCT.Models.DUT.PA45_MainBoard;
using Microsoft.VisualBasic;

namespace FCT.Service.DUT.Service
{
    public static class HON_Printer_Log_Parser
    {
        public static string get_Media_Type(this Media_Type mediaType)
        {
            string m_MediaType = string.Empty;
            switch (mediaType)
            {
                case Media_Type.Gaps:
                    m_MediaType = "Media With Gaps";
                    break;

                case Media_Type.BlackMark:
                    m_MediaType = "Black Mark";
                    break;

                case Media_Type.ContinuousVarLen:
                    m_MediaType = "Continuous Var Len";
                    break;
            }
            return m_MediaType;
        }

        public static void get_HEAD_LIFT_SENSOR(this IDUT m_dut, string text, ref string sensorVal)
        {
            string name = m_dut.GetType().Name;

            if (name != "PA45" && name != "PA45_MainBoard")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Print head"))
                {
                    sensorVal = v.Replace("Print head", "").Trim();
                }
            }
        }

        public static void get_LTS(this IDUT m_dut, string text, ref string sensorVal)
        {
            string name = m_dut.GetType().Name;

            if (name != "PA45" && name != "PA45_MainBoard" && name != "LTSSensor")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Sensor Value:"))
                {
                    sensorVal = v.Replace("Sensor Value:", "").Trim();
                }
            }
        }

        public static void get_BM(this IDUT m_dut, string text, ref string sensorVal, ref string drive, ref string gain)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45" && name != "PA45_MainBoard")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Sensor Value:")) { sensorVal = v.Replace("Sensor Value:", "").Trim(); }
                if (v.Contains("Drive:")) { drive = v.Replace("Drive:", "").Trim(); }
                if (v.Contains("Gain:")) { gain = v.Replace("Gain:", "").Trim(); }
            }
        }

        public static void get_LSS(this IDUT m_dut, string text, ref string sensorVal, ref string drive, ref string gain)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45" && name != "PA45_MainBoard")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Sensor Value:")) { sensorVal = v.Replace("Sensor Value:", "").Trim(); }
                if (v.Contains("Drive:")) { drive = v.Replace("Drive:", "").Trim(); }
                if (v.Contains("Gain:")) { gain = v.Replace("Gain:", "").Trim(); }
            }
        }

        public static void get_TPH_ID(this IDUT m_dut, string text, ref string part, ref string seri)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Part Number:")) { part = v.Replace("Part Number:", "").Trim(); }
                if (v.Contains("Serial Number:")) { seri = v.Replace("Serial Number:", "").Trim(); }
            }
        }

        public static void get_BLUETOOTH_ID(this IDUT m_dut, string text, ref string part, ref string seri, ref string db)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45" && name != "WiFi_BT_Module")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Part Number:")) { part = v.Replace("Part Number:", "").Trim(); }
                if (v.Contains("Serial Number:")) { seri = v.Replace("Serial Number:", "").Trim(); }
                if (v.Contains("BD Address:")) { db = v.Replace("BD Address:", "").Trim(); }
            }
        }

        public static void get_WIFI_ID(this IDUT m_dut, string text, ref string part, ref string seri, ref string mac)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45" && name != "WiFi_BT_Module")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Part Number:")) { part = v.Replace("Part Number:", "").Trim(); }
                if (v.Contains("Serial Number:")) { seri = v.Replace("Serial Number:", "").Trim(); }
                if (v.Contains("MAC Address:")) { mac = v.Replace("MAC Address:", "").Trim(); }
            }
        }

        public static void get_printer_rfid(this IDUT m_dut, string text, ref string part, ref string seri, ref string region)
        {
           
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("PartNumber=")) { part = v.Replace("PartNumber=", "").Trim(); }
                if (v.Contains("SerialNumber=")) { seri = v.Replace("SerialNumber=", "").Trim(); }
                if (v.Contains("Region=")) { region = v.Replace("Region=", "").Trim(); }
            }
        }



        public static void get_BATTERY_TEST(this IDUT m_dut, string text, ref string status, ref string externalPower, ref string remaining, ref string health, ref string current)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Status:")) { status = v.Replace("Status:", "").Trim(); }
                if (v.Contains("ExternalPower:")) { externalPower = v.Replace("ExternalPower:", "").Trim(); }
                if (v.Contains("Remaining:")) { remaining = v.Replace("Remaining:", "").Trim(); }
                if (v.Contains("Health:")) { health = v.Replace("Health:", "").Trim(); }
                if (v.Contains("Current:")) { current = v.Replace("Current:", "").Trim(); }
            }
        }

        public static void get_printer_sensor_value(this IDUT m_dut, string text, ref string value)
        {

        }

        public static void get_printer_sensor_templture(this IDUT m_dut, string text, PA45_Sensor sensor, ref string temp)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45"&& name != "PA45_MainBoard")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains(sensor.ToString()) && v.Contains("Temp="))
                {
                    int index = v.IndexOf("Temp=");

                    if (index != -1)
                    {

                        temp = v.Substring(index + ("Temp=").Length, v.Length);
                        return;
                    }
                }
            }
        }

        public static void get_modules_printer(this IDUT m_dut, string text, ref string part, ref string seri, ref string radio, ref string region)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Part Num =")) { part = v.Replace("Part Num =", "").Trim(); }
                if (v.Contains("Serial Num =")) { seri = v.Replace("Serial Num =", "").Trim(); }
                if (v.Contains("Radio =")) { radio = v.Replace("Radio =", "").Trim(); }
                if (v.Contains("Region =")) { region = v.Replace("Region =", "").Trim(); }
            }
        }

        public static void get_modules_cpu(this IDUT m_dut, string text, ref string part, ref string seri, ref string mech)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Part Num =")) { part = v.Replace("Part Num =", "").Trim(); }
                if (v.Contains("Serial Num =")) { seri = v.Replace("Serial Num =", "").Trim(); }
                if (v.Contains("Mech =")) { mech = v.Replace("Mech =", "").Trim(); }
            }
        }

        public static void get_WIFI_IPADDRESS(this IDUT m_dut, string text, ref string address)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("WIFI IPADDRESS :"))
                {
                    address = v.Replace("WIFI IPADDRESS :", "").Trim();
                    break;
                }
            }
        }

        public static void get_SecureBoot(this IDUT m_dut, string text, ref string bootStatus)
        {
            //console=ttymxc0,115200 SecureBoot=Disabled cma=32M root=/dev/mmcblk1p2 rootfstype=ext4 rootwait rw
            string name = m_dut.GetType().Name;
            if (name != "PA45")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split(' ').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string var in values)
            {
                if (var.Contains("SecureBoot"))
                {
                    bootStatus = var.Replace("SecureBoot=", "").Trim();
                    break;
                }
            }
        }

        public static void get_PRINTHEAD(this IDUT m_dut, string text, ref string ph_DPI)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string var in values)
            {
                if (var.Contains("Print Head DPI:"))
                {
                    ph_DPI = var.Replace("Print Head DPI:", "").Trim();
                    break;
                }
            }
        }

        public static void get_CHARGER(this IDUT m_dut, string text, ref string reg)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split(' ').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string var in values)
            {
                if (var.Contains("REG"))
                {
                    reg = var.Replace("REG", "").Trim();
                    break;
                }
            }
        }

        public static void get_MOTION(this IDUT m_dut, string text, ref string whoIam)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45" && name != "PA45_MainBoard")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split(' ').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string var in values)
            {
                if (var.Contains("WHO_AM_I="))
                {
                    whoIam = var.Replace("WHO_AM_I=", "").Trim();
                    break;
                }
            }
        }

        public static void get_Memtool_Register_Hex_Value(this IDUT m_dut, string text, string address, ref string value)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45_MainBoard")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string var in values)
            {

                if (var.Contains(address + ":"))
                {
                    value = var.Replace(address, "").Replace(":", "").Trim();
                    break;
                }
            }

        }

        public static void get_Memtool_Register_Char_Value(this IDUT m_dut, string hexString, int index, ref char value)
        {
            string name = m_dut.GetType().Name;
            if (name != "PA45_MainBoard")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            char[] chars = hexString.ToCharArray();
            value = chars[index];
        }

        public static void get_FW_Version(this IDUT m_dut, string text, ref string sensorVal)
        {
            string name = m_dut.GetType().Name;

            if (name != "PA45" && name != "PA45_MainBoard")
            {
                throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut));
            }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                string tmpVal = v.Replace(PA45_FCT_Commands.root_OK_ConsoleTag, "");
                if (!string.IsNullOrWhiteSpace(tmpVal))
                {
                    sensorVal = tmpVal;
                    return;
                }
            }
        }

        public static void get_RFID_ID(this IDUT m_dut, string text, ref string partNum, ref string seriNum)
        {
            string name = m_dut.GetType().Name;

            if (name != "PA45") { throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut)); }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                if (tmp.Contains("Part Number:")) { partNum = tmp.Replace("Part Number:", "").Trim(); }
                if (tmp.Contains("Serial Number:")) { seriNum = tmp.Replace("Serial Number:", "").Trim(); }
            }
        }

        public static void get_RFID_CC(this IDUT m_dut, string text, ref string cc)
        {
            string name = m_dut.GetType().Name;

            if (name != "PA45") { throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut)); }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                if (tmp.Contains("MHz") && tmp.Contains("CC"))
                {
                    string[] rfid_ifs = tmp.Split(' ').ToArray();

                    foreach (string info in rfid_ifs)
                    {
                        if (info.StartsWith("CC") && info.Length == 4)
                        {
                            cc = info.Replace("CC", "");
                            return;
                        }
                    }
                }
            }
        }

        public static void get_tph_resi(this IDUT m_dut, string text, ref string res)
        {
            string name = m_dut.GetType().Name;

            if (name != "PA45_MainBoard") { throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut)); }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                if (tmp.Contains("ohm"))
                {
                    res = tmp.Replace("100:", "").Replace("ohm", "");
                    return;
                }
            }
        }

        public static void get_MFI_INFO(this IDUT m_dut, string text, ref string devVer, ref string devID, ref string authRev, ref string dauthProtVerevVer)
        {
            // string devVer = "", devID = "", authRev = "", authProtVer = "";
            string name = m_dut.GetType().Name;

            if (name != "PA45_MainBoard") { throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut)); }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                if (tmp.Contains("ohm"))
                {
                    //res = tmp.Replace("100:", "").Replace("ohm", "");
                    return;
                }
            }
        }

        public static void get_uart_mcu_cat(this IDUT m_dut, string text,ref string catVal )
        {
            // string devVer = "", devID = "", authRev = "", authProtVer = "";
            string name = m_dut.GetType().Name;

            if (name != "PA45_MainBoard") { throw new ArgumentException($"m_dut can't be {name}", nameof(m_dut)); }

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                string catValue = tmp.Trim();
                if (Information.IsNumeric(catValue))
                {
                    catVal = catValue;
                    return;
                }

            }
        }

        public static void get_usb_device(this IDUT m_dut, string text, ref List<string> deveList)
        {

            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                if (tmp.Contains("sd"))
                {
                    string tmpStr = tmp.Replace(" ", "");
                    tmpStr = tmpStr.Replace("sd", ";");
                    deveList = tmpStr.Split(';').ToList();
                    deveList.RemoveAll(s => string.IsNullOrWhiteSpace(s));
                    return;
                }
            }
        }

        public static bool get_i2c_motion_result(this IDUT m_dut, string text, ref string result)
        {
            //bool rlt = false;
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                string tmpResult = tmp.Replace("MOTION", "").Replace("OK", "").Replace("FAIL", "").Replace("(0x44)", "").Trim();

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("OK"))
                {
                    result = tmp;
                    return true;
                }

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("FAIL"))
                {
                    result = tmp;
                    return false;
                }
            }

            return false;
        }

        public static bool get_i2c_security_result(this IDUT m_dut, string text, ref string result)
        {
            //bool rlt = false;
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                string tmpResult = tmp.Replace("SECURITY", "").Replace("OK", "").Replace("FAIL", "").Trim();

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("OK"))
                {
                    result = tmp;
                    return true;
                }

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("FAIL"))
                {
                    result = tmp;
                    return false;
                }
            }

            return false;
        }

        public static bool get_i2c_rtc_result(this IDUT m_dut, string text, ref string result)
        {
            //bool rlt = false;
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                string tmpResult = tmp.Replace("RTC", "").Replace("OK", "").Replace("FAIL", "").Trim();

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("OK"))
                {
                    result = tmp;
                    return true;
                }

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("FAIL"))
                {
                    result = tmp;
                    return false;
                }
            }

            return false;
        }

        public static bool get_i2c_mfi_result(this IDUT m_dut, string text, ref string result)
        {
            //bool rlt = false;
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                string tmpResult = tmp.Replace("MFI", "").Replace("OK", "").Replace("FAIL", "").Trim();

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("OK"))
                {
                    result = tmp;
                    return true;
                }

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("FAIL"))
                {
                    result = tmp;
                    return false;
                }
            }

            return false;
        }

        public static bool get_i2c_charger_result(this IDUT m_dut, string text, ref string result)
        {
            //bool rlt = false;
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                string tmpResult = tmp.Replace("CHARGER", "").Replace("OK", "").Replace("FAIL", "").Trim();

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("OK"))
                {
                    result = tmp;
                    return true;
                }

                if (string.IsNullOrWhiteSpace(tmpResult) && tmp.Contains("FAIL"))
                {
                    result = tmp;
                    return false;
                }
            }

            return false;
        }


        public static void get_public_key(this IDUT m_dut, string text, ref string model, ref string seri_num, ref string pub_key)
        {
            //bool rlt = false;
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string tmp in values)
            {
                if (tmp.Contains("Model ID:"))
                {
                    model = tmp.Replace("Model ID:", "").Trim();
                }

                if (tmp.Contains("Serial Number:"))
                {
                    seri_num = tmp.Replace("Serial Number:", "").Trim();
                }
            }

            string begin_flag = "-----BEGIN PUBLIC KEY-----", end_flag = "-----END PUBLIC KEY-----";
            int index_begin = text.IndexOf(begin_flag);
            int index_end = text.IndexOf(end_flag);
            string tmp_pub_key = text.Substring(index_begin + begin_flag.Length, index_end - index_begin - begin_flag.Length);
            pub_key = tmp_pub_key.Trim();
        }
    }
}
