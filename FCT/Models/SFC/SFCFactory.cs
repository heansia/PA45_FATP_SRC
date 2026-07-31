using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.SFC.Interface;

namespace FCT.Models.SFC

{
    public class SFCFactory
    {
        public static ISFC CreateSFCInstance()
        {
            return new SSG_SFC();
        }
    }
}
