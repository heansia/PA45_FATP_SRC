using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

using INI = FCT.Utilities.IniFileHelper;

namespace FCT.Models.DUT.USB2ETH
{
    class USB2ETH : DUT
    {
        public USB2ETH(IProduct product) : base(product)
        {
            Product = product;
        }

        public override void Test()
        {
            if (TestMethod != null)
            {

            }
        }

        public override string VID { get { return string.Empty; } }
        public override string PID { get { return string.Empty; } }
    }
}
