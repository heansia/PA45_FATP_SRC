using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Instrument;

namespace FCT.Extension
{
    public static class IntExtension
    {
        /// <summary>
        /// true = 1, false =0.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool check_bit_0_or_1(this int c, BitPosition position)
        {
            int mask = 0;

            if (position == BitPosition.zeroth)
            {
                mask = 0x1;
            }
            if (position == BitPosition.first)
            {
                mask = 0x2;
            }
            if (position == BitPosition.second)
            {
                mask = 0x4;
            }
            if (position == BitPosition.third)
            {
                mask = 0x8;
            }

            return !((c & mask) == 0);

        }

        /*
        public static DI_Status convert_to_di(this int c, BitPosition position)
        {
           
        }
        */
    }

    public enum BitPosition
    {
        error = -1,
        zeroth = 0,
        first = 1,
        second = 2,
        third = 3
    }
}
