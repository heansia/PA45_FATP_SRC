using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.DUT.WiFi_BT_Module
{
    class WiFi_BT_Module : DUT
    {
        public WiFi_BT_Module(IProduct product) : base(product) { Product = product; }

        public override void Test() { if (TestMethod != null) { } }

        public override string VID { get { return "0C2E"; } }
        public override string PID { get { return "FE32"; } }
    }

}
