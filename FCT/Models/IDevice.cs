using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models
{
    interface IDevice
    {
        string VID { get; set; }
        string PID { get; set; }
    }
}
