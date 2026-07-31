using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.DUT.TopSensor
{
    class TopSensor : DUT
    {
        public TopSensor(IProduct product) : base(product) { Product = product; }

        public override void Test() { if (TestMethod != null) { } }

        public override string VID { get { return string.Empty; } }
        public override string PID { get { return string.Empty; } }
    }


    public static class TopSensorExt
    {

    }



}
