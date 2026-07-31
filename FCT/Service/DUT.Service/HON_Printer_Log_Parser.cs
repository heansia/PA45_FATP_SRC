using System;
using System.Collections.Generic;
using System.Linq;
using FCT.Models.DUT.Interface;
using FCT.Models.DUT.PA45;

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
                    m_MediaType = "Gaps";
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
                if (v.Contains("Health:")) { remaining = v.Replace("Health:", "").Trim(); }
                if (v.Contains("Current:")) { remaining = v.Replace("Current:", "").Trim(); }
            }
        }

        public static void get_printer_sensor(this IDUT m_dut, string text)
        {

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

    }
}
