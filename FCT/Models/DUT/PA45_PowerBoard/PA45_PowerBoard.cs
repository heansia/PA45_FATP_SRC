using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.DUT.PA45_PowerBoard
{
    class PA45_PowerBoard : DUT
    {
        public PA45_PowerBoard(IProduct product) : base(product)
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
