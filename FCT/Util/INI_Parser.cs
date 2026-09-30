using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Util
{
    static class INI_Parser
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);

        private static string GetValueByKeyInSectionFromIniFile(string section, string key, string iniFile)
        {
            StringBuilder value = new StringBuilder();
            GetPrivateProfileString(section, key, "-1", value, 500, iniFile);

            return value.ToString();
        }

        public static string GetValue(string section, string key, string iniFile)
        {
            return GetValueByKeyInSectionFromIniFile(section, key, iniFile);
        }

        public static int GetIntValue(string section, string key, string iniFile)
        {
            return Convert.ToInt32(GetValueByKeyInSectionFromIniFile(section, key, iniFile));
        }

        [DllImport("kernel32")]

        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        private static void SetValueByKeyInSectionFromIniFile(string section, string key, string value, string iniFile)
        {
            WritePrivateProfileString(section, key, value, iniFile);
        }

        public static void SetValue(string section, string key, string value, string iniFile)
        {
            SetValueByKeyInSectionFromIniFile(section, key, value, iniFile);
        }
        
    }
}
