using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.Instrument.Interface
{
    interface ICaptureCard
    {
        double MeasureCurrent(int channelIndex);

        double MeasureVoltage(int channelIndex);
    }
}
