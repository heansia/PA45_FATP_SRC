using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Extension
{
    public static class StringExtension
    {
        public static bool IsNullOrEmptyOrWhiteSpace(this string mString)
        {
            return string.IsNullOrEmpty(mString) && string.IsNullOrWhiteSpace(mString);
        }

        public static double d2a(this string mStr, int bitCount, double refVoltage) 
        {
            int denominator = 1 << bitCount;
            int numerator = Convert.ToInt32(mStr);
            double analogVal = ((double)numerator / (double)denominator) * refVoltage;

            return analogVal;
        }

    }
}
