using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.Products.Interface
{
    public interface IProductConfig
    {
        string PROJECT { get; set; }
        string MODEL { get; set; }
        string MODEL_NO { get; set; }
        string DATABASE { get; set; }
        string LINE_CODE { get; set; }
        string TABLE { get; set; }
        string SN_START_WITH { get; set; }
        string SN_END_WITH { get; set; }
        string SN_LENGTH { get; set; } // 允许空值
        string PN_START_WITH { get; set; }
        string PN_END_WITH { get; set; }
        string PN_LENGTH { get; set; }
        string STATION_CODE { get; set; }
        string TEST_ITEMS { get; set; }

    }
}
