using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.Instrument.Interface
{
    interface IRelayCard : IInstrument
    {
        bool SetRelayOn(int channelIndex);
        bool SetRelayOff(int channelIndex);
        void SetAllRelayOn();
        void SetAllRelayOff();
    }
}
