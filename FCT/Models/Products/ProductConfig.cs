using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.Products
{
    public class ProductConfig : IProductConfig
    {
        public ProductConfig() { }

        private string _model_no = "";

        public string PROJECT { get; set; }
        public string MODEL { get; set; }
        public string MODEL_NO { get; set; }

        public string LINE_CODE
        {
            get { return _model_no; }

            set { _model_no = value.Replace("PMX-", ""); }
        }

        public string DATABASE { get; set; }
        public string TABLE { get; set; }
        public string SN_START_WITH { get; set; }
        public string SN_END_WITH { get; set; }
        public string SN_LENGTH { get; set; }
        public string PN_START_WITH { get; set; }
        public string PN_END_WITH { get; set; }
        public string PN_LENGTH { get; set; }
        public string STATION_CODE { get; set; }
        public string TEST_ITEMS { get; set; }
    }
}
