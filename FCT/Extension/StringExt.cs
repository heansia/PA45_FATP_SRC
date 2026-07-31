using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Extension
{
    public static class StringExt
    {
        public static bool IsNullOrEmptyOrWhiteSpace(this string mString)
        {
            return string.IsNullOrEmpty(mString) && string.IsNullOrWhiteSpace(mString);
        }
    }
}
