using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Extension
{
    public static class CharExtension
    {
        public static int ascii_2_nibble(this char c)
        {
            if (c >= '0' && c <= '9')

            {
                return c - '0';
            }
            else if (c >= 'A' && c <= 'F') { return c - 'A' + 10; }
            else if (c >= 'a' && c <= 'f') { return c - 'a' + 10; }
            else { return 0; }
        }


     
    }

    
}
