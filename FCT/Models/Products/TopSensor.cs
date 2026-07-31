using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.Products
{
    class TopSensor : Product
    {
        public TopSensor(string sn, IProductConfig productConfig) : base(sn, productConfig) { }

        public TopSensor() : base() { }

        public override bool CheckBarcodeFormat() { return base.CheckBarcodeFormat(); }
    }
}
