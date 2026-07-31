using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.Instrument.Interface
{
    public interface IInstrument
    {
        bool Open();
        bool Close();
    }
}
