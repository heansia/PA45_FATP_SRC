using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.SFC.Interface
{
    public interface ISFC
    {
        string CheckStationPass(string Product, string Station, string Barcode, string LineNo, string Version);
        string GetSpecialValue(string Product, string Type, string Parameters);
        string InsertIntoTable(string Product, string TableName, string ColumnList, string ValueList);
        string GetDbNow(string Product);
    }
}
