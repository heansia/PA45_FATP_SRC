using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.SFC.Interface;
using SFCDLL_CommonEx.Interface;

namespace FCT.Models.SFC
{
    public class SSG_SFC : ISFC
    {
        private ISFCDLL sfc;
        
        public SSG_SFC()
        {
            if (sfc == null)
            {
                sfc = new SFCDLL();
            }
                
        }

        public string CheckStationPass(string Product, string Station, string Barcode, string LineNo, string Version)
        {
            return sfc.CheckStationPass(Product, Station, Barcode, LineNo, Version);
        }

        public string CheckStationPass(string barcode, SFCS_Setting setting)
        {
            return sfc.CheckStationPass
                (
                setting.Product,
                setting.Station,
                barcode,
                setting.Line,
                setting.testVersion
                );
        }

        public string GetDbNow(string Product)
        {
            return sfc.GetDbNow(Product);
        }

        public string GetSpecialValue(string Product, string Type, string Parameters)
        {
            return sfc.GetSpecialValue(Product, Type, Parameters);
        }

        public string InsertIntoTable(string Product, string TableName, string ColumnList, string ValueList)
        {
            return sfc.InsertIntoTable(Product, TableName, ColumnList, ValueList);
        }
    }
}
