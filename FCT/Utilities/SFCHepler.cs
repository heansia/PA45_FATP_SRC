using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFCDLL_CommonEx.Interface;

namespace FCT.Utilities
{
    public static class SFCSHelper
    {
        private static ISFCDLL sfc;

        public static bool CheckStationPass(string Product, string Station, string Barcode, string LineNo, string Version,ref string out_log)
        {
            if (sfc == null)
                sfc = new SFCDLL();

            out_log = sfc.CheckStationPass(Product, Station, Barcode, LineNo, Version);
            MESMessageAlert?.Invoke(new MESMessageAlertEventArgs(out_log));
            return sfc.CheckStationPass(Product, Station, Barcode, LineNo, Version) == "Y";
        }
        
        public static bool InsertIntoTable(string Product, string TableName, string ColumnList, string ValueList, ref string out_log)
        {
            if (sfc == null)
                sfc = new SFCDLL();

            out_log = sfc.InsertIntoTable(Product, TableName, ColumnList, ValueList);
            MESMessageAlert?.Invoke(new MESMessageAlertEventArgs(out_log));
            return sfc.InsertIntoTable(Product, TableName, ColumnList, ValueList) == "Y";
        }
        
        public static string GetSpecialValue(string Product, string Type, string Parameters)
        {
            if (sfc == null)
                sfc = new SFCDLL();

            return sfc.GetSpecialValue(Product, Type, Parameters);
        }

        public static string GetDbNow(string Product)
        {
            if (sfc == null)
                sfc = new SFCDLL();

            return sfc.GetDbNow(Product);
        }

        public static event MESMessageAlertEventHanlder MESMessageAlert;

    }
    
}
