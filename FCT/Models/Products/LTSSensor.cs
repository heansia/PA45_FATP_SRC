using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.Products
{
    class LTSSensor : Product
    {
        public LTSSensor(string sn, IProductConfig productConfig) : base(sn, productConfig) { }

        public LTSSensor() : base() { }

        public override bool CheckBarcodeFormat() { return base.CheckBarcodeFormat(); }
    }
}
