using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.DUT.PA45
{
    public static class Media_Type_Ext
    {
        public static string get_Media_Type(this Media_Type mediaType)
        {
            string m_MediaType = string.Empty;
            switch (mediaType)
            {
                case Media_Type.Gaps:
                    m_MediaType = "Gaps";
                    break;

                case Media_Type.BlackMark:
                    m_MediaType = "Black Mark";
                    break;

                case Media_Type.ContinuousVarLen:
                    m_MediaType = "Continuous Var Len";
                    break;
            }
            return m_MediaType;
        }
    }
}
