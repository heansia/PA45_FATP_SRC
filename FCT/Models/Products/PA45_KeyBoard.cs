using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.Products
{
    class PA45_KeyBoard : Product
    {
        public PA45_KeyBoard(string sn, IProductConfig productConfig) : base(sn, productConfig) { }

        public PA45_KeyBoard() : base() { }

        public override bool CheckBarcodeFormat() { return base.CheckBarcodeFormat(); }
    }
}
