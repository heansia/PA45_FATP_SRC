using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using FCT.Util;

using CSV = FCT.Util.CsvHelper;

namespace FCT
{
    public static class Global
    {
        public static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;

        public static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static readonly string CONFIG_FILE = Path.Combine(ASSEMBLY_PATH, "Config.ini");

        public static readonly string TEST_ITEMS_FILE = Path.Combine(ASSEMBLY_PATH, "TestItems.ini");

        public static readonly string NewLine = Environment.NewLine;

        public static readonly string MICROCHIP_MANUFACTURING_TOOL;

        public static readonly string MICROCHIP_MANUFACTURING_TOOL_PATH;

        public static readonly string WIFI_MAC_LAB_FILE = Path.Combine(ASSEMBLY_PATH, "LabFile", "wifi_mac.Lab");

        public static readonly string BLUETOOTH_MAC_LAB_FILE = Path.Combine(ASSEMBLY_PATH, "LabFile", "bluetooth_mac.Lab");

        public static readonly string PRODUCT_SETTING_MATRIX_PATH = Path.Combine(ASSEMBLY_PATH, "ProductMatrix.csv");

        public static readonly DataTable PRODUCT_SETTING_MATRIX = CSV.CsvToDataTable(PRODUCT_SETTING_MATRIX_PATH);


        public static readonly string pic_pa45_put_gap_media = "";

        public static readonly string pic_pa45_testfeed_gap_media = "";

        public static readonly string pic_pa45_put_black_mark_inside = "";

        public static readonly string pic_pa45_testfeed_black_mark_inside = "";

        public static readonly string pic_pa45_put_black_mark_outside = "";

        public static readonly string pic_pa45_testfeed_black_mark_outside = "";

        public static readonly string pic_pa45_put_nfc_reader_on_printer = "";
    }
}

