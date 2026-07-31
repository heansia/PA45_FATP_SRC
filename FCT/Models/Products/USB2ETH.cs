using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.Products
{
    class USB2ETH : Product
    {
        public USB2ETH(string sn, IProductConfig productConfig) : base(sn, productConfig) { }

        public USB2ETH() : base() { }

        public override bool CheckBarcodeFormat() { return base.CheckBarcodeFormat(); }
    }
}
