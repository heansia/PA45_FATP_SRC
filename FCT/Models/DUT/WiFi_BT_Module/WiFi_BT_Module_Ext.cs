using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.DUT.Interface;

namespace FCT.Models.DUT.WiFi_BT_Module
{
    public static class WiFi_BT_Module_Ext
    {
        public static void get_BLUETOOTH_ID(this IDUT m_wifi_bt, string text, ref string part, ref string seri, ref string db)
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

        public static void get_WIFI_ID(this WiFi_BT_Module m_wifi_bt, string text, ref string part, ref string seri, ref string mac)
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
    }

}
