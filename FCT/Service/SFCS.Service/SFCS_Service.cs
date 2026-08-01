using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.SFC.Interface;

namespace FCT.Service.SFCS.Service
{
    public static class SFCS_Service
    {
        public static string get_Main_Product_Subpart(this ISFC sfcInstance, string pName, string pSN, string subpartModelName)
        {
            return sfcInstance.GetSpecialValue(pName, "GET_BARCODE_PART", pSN + $";{subpartModelName}");
        }

        public static string get_Sub_Product_Mainpart(this ISFC sfcInstance, string pName, string pSN, string subpartModelName)
        {
            return sfcInstance.GetSpecialValue(pName, "GET_BARCODE_PART", pSN + $";{subpartModelName}");
        }

        public static string get_PN_Part(this ISFC sfcInstance)
        {
            return "";
        }


    }
}
