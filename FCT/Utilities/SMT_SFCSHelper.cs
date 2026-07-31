using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Primax.SFCS.DLL;
namespace FCT.Utilities
{

    public class SMT_SFCSHelper
    {
        private static SMTDLL smtSfc;
        public static bool CheckStationPass(string Product, string Station, string Barcode, string LineNo, string Version, ref string out_log)
        {
            if (smtSfc == null)
                smtSfc = new SMTDLL();
            out_log = smtSfc.CheckStationPass(Product, Station, Barcode, LineNo, Version);

            MESMessageAlert?.Invoke(new MESMessageAlertEventArgs(out_log));
            return out_log == "Y" ? true : false;
        }
        public static bool InsertIntoTable(string Product, string TableName, string ColumnList, string ValueList, ref string out_log)
        {
            if (smtSfc == null)
                smtSfc = new SMTDLL();
            out_log = smtSfc.InsertIntoTable(Product, TableName, ColumnList, ValueList);
            MESMessageAlert?.Invoke(new MESMessageAlertEventArgs(out_log));
            return out_log == "Y" ? true : false;
        }

        public static event MESMessageAlertEventHanlder MESMessageAlert;
    }


}
