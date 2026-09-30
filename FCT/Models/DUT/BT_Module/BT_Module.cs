using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.DUT.BT_Module
{
    class BT_Module : DUT
    {
        public static bool RunOsCmdEnable = false;
        public BT_Module(IProduct product) : base(product)
        {
            Product = product;
        }

        public override void Test()
        {
            if (TestMethod != null)
            {

            }
        }

        public override bool FinalResult
        {

            get
            {
                RunOsCmdEnable = false;
                return base.FinalResult;
            }
        }

        //USB\VID_0C2E&PID_FE32&MI_00\6&3AF21B86&0&0000

        public override string VID { get { return "0C2E"; } }
        public override string PID { get { return "FE32"; } }
    }

}
