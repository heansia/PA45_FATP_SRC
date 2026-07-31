using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.DUT.PA45
{
    public static class PA45_Ext
    {
        public static void get_HEAD_LIFT_SENSOR(this PA45 m_PA45, string text, ref string sensorVal)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values) { if (v.Contains("Print head")) { sensorVal = v.Replace("Print head", "").Trim(); } }
        }

        public static void get_LTS(this PA45 m_PA45, string text, ref string sensorVal)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values) { if (v.Contains("Sensor Value:")) { sensorVal = v.Replace("Sensor Value:", "").Trim(); } }
        }

        public static void get_BM(this PA45 m_PA45, string text, ref string sensorVal, ref string drive, ref string gain)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Sensor Value:")) { sensorVal = v.Replace("Sensor Value:", "").Trim(); }
                if (v.Contains("Drive:")) { drive = v.Replace("Drive:", "").Trim(); }
                if (v.Contains("Gain:")) { gain = v.Replace("Gain:", "").Trim(); }
            }
        }

        public static void get_LSS(this PA45 m_PA45, string text, ref string sensorVal, ref string drive, ref string gain)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Sensor Value:")) { sensorVal = v.Replace("Sensor Value:", "").Trim(); }
                if (v.Contains("Drive:")) { drive = v.Replace("Drive:", "").Trim(); }
                if (v.Contains("Gain:")) { gain = v.Replace("Gain:", "").Trim(); }
            }
        }

        public static void get_TPH_ID(this PA45 m_PA45, string text, ref string part, ref string seri)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Part Number:")) { part = v.Replace("Part Number:", "").Trim(); }
                if (v.Contains("Serial Number:")) { seri = v.Replace("Serial Number:", "").Trim(); }
            }
        }

        public static void get_BLUETOOTH_ID(this PA45 m_PA45, string text, ref string part, ref string seri, ref string db)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Part Number:")) { part = v.Replace("Part Number:", "").Trim(); }
                if (v.Contains("Serial Number:")) { seri = v.Replace("Serial Number:", "").Trim(); }
                if (v.Contains("BD Address:")) { db = v.Replace("BD Address:", "").Trim(); }
            }
        }

        public static void get_WIFI_ID(this PA45 m_PA45, string text, ref string part, ref string seri, ref string mac)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Part Number:")) { part = v.Replace("Part Number:", "").Trim(); }
                if (v.Contains("Serial Number:")) { seri = v.Replace("Serial Number:", "").Trim(); }
                if (v.Contains("MAC Address:")) { mac = v.Replace("MAC Address:", "").Trim(); }
            }
        }

        public static void get_BATTERY_TEST(this PA45 m_PA45, string text, ref string status, ref string externalPower, ref string remaining, ref string health, ref string current)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Status:")) { status = v.Replace("Status:", "").Trim(); }
                if (v.Contains("ExternalPower:")) { externalPower = v.Replace("ExternalPower:", "").Trim(); }
                if (v.Contains("Remaining:")) { remaining = v.Replace("Remaining:", "").Trim(); }
                if (v.Contains("Health:")) { remaining = v.Replace("Health:", "").Trim(); }
                if (v.Contains("Current:")) { remaining = v.Replace("Current:", "").Trim(); }
            }
        }

        public static void get_printer_sensor(this PA45 m_PA45, string text)
        {

        }
        
        public static void get_modules_printer(this PA45 m_PA45, string text, ref string part, ref string seri, ref string radio, ref string region)
        {
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

        public static void get_modules_cpu(this PA45 m_PA45, string text, ref string part, ref string seri, ref string mech)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Part Num =")) { part = v.Replace("Part Num =", "").Trim(); }
                if (v.Contains("Serial Num =")) { seri = v.Replace("Serial Num =", "").Trim(); }
                if (v.Contains("Mech =")) { mech = v.Replace("Mech =", "").Trim(); }
            }
        }

    }
}
