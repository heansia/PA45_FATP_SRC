using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.DUT.LTSSensor
{

    public class LTSSensor : DUT
    {
        public LTSSensor(IProduct product) : base(product)
        {
            Product = product;
        }

        public override void Test()
        {
            if (TestMethod != null)
            {

            }
        }

        public override string VID { get { return "0C2E"; } }
        public override string PID { get { return "FE32"; } }
    }

    public static class LTSSensor_Ext
    {
        public static void get_LTS(this LTSSensor m_LTS, string text, ref string sensorVal)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Sensor Value:"))
                {
                    sensorVal = v.Replace("Sensor Value:", "").Trim();
                    break;
                }
            }
        }
    }
}
