using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.SFC.Interface;
using Primax.SFCS.DLL;
using SFCDLL_CommonEx.Interface;

namespace FCT.Models.SFC
{
   public class SMT_SFC : ISFC
    {
        private SMTDLL sfc;


        public SMT_SFC()
        {
            if (sfc == null)
                sfc = new SMTDLL();
        }

        public SMT_SFC(SFCS_Setting setting)
        {
            if (sfc == null)
                sfc = new SMTDLL();
        }

        public string CheckStationPass(string Product, string Station, string Barcode, string LineNo, string Version)
        {
            throw new NotImplementedException();
        }

        public string GetDbNow(string Product)
        {
            throw new NotImplementedException();
        }

        public string GetSpecialValue(string Product, string Type, string Parameters)
        {
            throw new NotImplementedException();
        }

        public string InsertIntoTable(string Product, string TableName, string ColumnList, string ValueList)
        {
            throw new NotImplementedException();
        }
    }
}
