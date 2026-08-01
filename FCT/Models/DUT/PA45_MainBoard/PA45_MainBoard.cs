using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;
using INI = FCT.Utilities.INI_Parser;

namespace FCT.Models.DUT.PA45_MainBoard
{
    class PA45_MainBoard : DUT
    {
        //USB\VID_0C2E&PID_0000&MI_00\6&1EBEE9E&0&0000
        public override string VID
        {
            get
            {
                string m_vid = INI.GetValue("Device", "VID", Global.CONFIG_FILE);
                if (string.IsNullOrEmpty(m_vid) ||
                    string.IsNullOrWhiteSpace(m_vid) ||
                    m_vid == "-1")
                {
                    return "0C2E";
                }
                else
                {
                    return m_vid;
                }

            }
        }
        public override string PID
        {
            get
            {
                string m_pid = INI.GetValue("Device", "PID", Global.CONFIG_FILE);
                if (string.IsNullOrEmpty(m_pid) ||
                    string.IsNullOrWhiteSpace(m_pid) ||
                    m_pid == "-1")
                {
                    return m_pid;
                }
                else
                {
                    return "0000";
                }


            }
        }

        public PA45_MainBoard(IProduct product) : base(product)
        {
            Product = product;
        }

        public override void Test()
        {
            if (TestMethod != null)
            {

            }
        }

    }


}
