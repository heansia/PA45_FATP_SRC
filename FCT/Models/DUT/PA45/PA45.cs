using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;
using INI = FCT.Utilities.IniFileHelper;

namespace FCT.Models.DUT.PA45
{
    public class PA45 : DUT
    {
        //USB\VID_0C2E&PID_0000&MI_00\6&2351AB96&0&0000

        public override string VID
        {
            get
            {
                string m_vid = INI.GetValue("Device", "VID", Global.CONFIG_FILE);
                if (string.IsNullOrEmpty(m_vid) || string.IsNullOrWhiteSpace(m_vid) || m_vid == "-1")
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
                    return "0000";
                }
                else
                {
                    return m_pid;
                }
            }
        }

        //USB\VID_0C2E&PID_FE41&REV_0612&MI_00
        //USB\VID_0C2E&PID_FE32&REV_0612&MI_00

        public PA45(IProduct product) : base(product)
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


    public enum uname_option
    {
        //uname -p
        a,  //Print all
        b,
        m,  //Machine (hardware) type
        n,  //Hostname
        r,  //Kernel release
        s,  //Kernel name (default)
        p,  //Processor type
        v,  //Kernel version
        i,  //Hardware platform
        o   //OS name
    }

    public static class uname_option_Ext
    {
        public static string PrintSystemInformation(this uname_option u_option)
        {
            return "run \"uname -" + u_option + "\"";
        }
    }

}

