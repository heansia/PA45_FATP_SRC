using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FCT.Models;
using FCT.Models.Instrument.Interface;
using FCT.Models.Products;
using FCT.Models.Products.Interface;
using FCT.Models.SFC;

namespace FCT
{
    public delegate void RunTimeLogUpdatedEventHanlder(object sender, RunTimeLogUpdatedEventArgs e);
    public class RunTimeLogUpdatedEventArgs : EventArgs
    {
        public string Message { get; }
        public bool IsAddTimestamp { get; }
        public RunTimeLogUpdatedEventArgs(string message, bool isAddTimestamp = false)
        {
            Message = message;
            IsAddTimestamp = isAddTimestamp;
        }
    }

    public delegate void MESSettingChangedEventHanlder(object sender, MESSettingChangedEventArgs e);
    public class MESSettingChangedEventArgs : EventArgs
    {
        public SFCS_Setting _sfcs_setting { get; }
        public MESSettingChangedEventArgs(SFCS_Setting sfcs_setting)
        {
            _sfcs_setting = sfcs_setting;
        }
    }

    public delegate void MESMessageAlertEventHanlder(MESMessageAlertEventArgs e);
    public class MESMessageAlertEventArgs : EventArgs
    {
        private string Message { get; }
        public MESMessageAlertEventArgs(string message)
        {
            Message = message;
        }
    }

    public delegate void KeyButtonTestedEventHanlder(object sender, KeyButtonTestedEventArgs e);
    public class KeyButtonTestedEventArgs : EventArgs
    {
        public List<string> KeyCodes { get; }
        public bool IsAddTimestamp { get; }
        public KeyButtonTestedEventArgs(List<string> keyCodes, bool isAddTimestamp = false)
        {
            KeyCodes = keyCodes;
            IsAddTimestamp = isAddTimestamp;
        }
    }

    public delegate void TestedEventHandler(object sender, TestedEventArgs e);
    public class TestedEventArgs : EventArgs
    {
        public TestItem _testItem { get; }
        public TestedEventArgs(TestItem item)
        {
            _testItem = item;
        }
    }

    public delegate void TestFinishedEventHandler(object sender, TestFinishedEventArgs e);
    public class TestFinishedEventArgs : EventArgs
    {
        public TestItem _testItem { get; }
        public TestFinishedEventArgs(TestItem item)
        {
            _testItem = item;
        }
    }

    public delegate void TestErrorStatusChangedEventHandler(object sender, TestErrorStatusChangedEventArgs e);
    public class TestErrorStatusChangedEventArgs : EventArgs
    {
        public Test_Proc_Error_Code ErrorCode { get; }
        public TestErrorStatusChangedEventArgs(Test_Proc_Error_Code errorCode)
        {
            ErrorCode = errorCode;
        }
    }

    public class ProductConfigChangedEventArgs : EventArgs
    {
        public IProductConfig ProductConfig { get; }
        public ProductConfigChangedEventArgs(IProductConfig productConfig)
        {
            ProductConfig = productConfig;
        }
    }

    public delegate void ProcessStartEventHandler(ProcessStartEventArgs e);
    public class ProcessStartEventArgs : EventArgs
    {
        public string Arguments { get; }
        public ProcessStartEventArgs(string args)
        {
            Arguments = args;
        }
    }

    public delegate string GetLineDelegate(string a, string b);
    public delegate string GetStringDelegate(string a, string b);

    public delegate void MeasureVoltageEventHanlder(object sender, MeasureVoltageEventArgs e);
    public class MeasureVoltageEventArgs : EventArgs
    {
        public IInstrument InstrumentType { get; }
        public int ChannelIndex { get; }
        public string Value { get; }
        public bool Result { get; }
        public MeasureVoltageEventArgs(IInstrument instrumentType, int channelIndex, string value, bool result)
        {
            InstrumentType = instrumentType;
            ChannelIndex = channelIndex;
            Value = value;
            Result = result;
        }
    }

    public delegate void MeasureCurrentEventHanlder(object sender, MeasureCurrentEventArgs e);
    public class MeasureCurrentEventArgs : EventArgs
    {
        public IInstrument InstrumentType { get; }
        public int ChannelIndex { get; }
        public string Value { get; }
        public bool Result { get; }
        public MeasureCurrentEventArgs(IInstrument instrumentType, int channelIndex, string value, bool result)
        {
            InstrumentType = instrumentType;
            ChannelIndex = channelIndex;
            Value = value;
            Result = result;
        }
    }

    public delegate void SetDAQCardDOEventHanlder(object sender, SetDAQCardDOEventArgs e);
    public class SetDAQCardDOEventArgs : EventArgs
    {
        public IInstrument InstrumentType { get; }
        public int ChannelIndex { get; }
        public string Value { get; }
        public bool Result { get; }
        public SetDAQCardDOEventArgs(IInstrument instrumentType, int channelIndex, string value, bool result)
        {
            InstrumentType = instrumentType;
            ChannelIndex = channelIndex;
            Value = value;
            Result = result;
        }
    }

    public delegate void GetDAQCardDIEventHanlder(object sender, GetDAQCardDIEventArgs e);
    public class GetDAQCardDIEventArgs : EventArgs
    {
        public IInstrument InstrumentType { get; }
        public int ChannelIndex { get; }
        public string Value { get; }
        public bool Result { get; }
        public GetDAQCardDIEventArgs(IInstrument relayType, int channelIndex, string value, bool result)
        {
            InstrumentType = relayType;
            ChannelIndex = channelIndex;
            Value = value;
            Result = result;
        }
    }

    public delegate void RelayOnEventHanlder(object sender, RelayOnEventArgs e);
    public class RelayOnEventArgs : EventArgs
    {
        public IInstrument InstrumentType { get; }
        public int ChannelIndex { get; }
        public string Value { get; }
        public bool Result { get; }
        public RelayOnEventArgs(IInstrument relayType, int channelIndex, string value, bool result)
        {
            InstrumentType = relayType;
            ChannelIndex = channelIndex;
            Value = value;
            Result = result;
        }
    }

    public delegate void RelayOffEventHanlder(object sender, RelayOffEventArgs e);
    public class RelayOffEventArgs : EventArgs
    {
        public IInstrument InstrumentType { get; }
        public int ChannelIndex { get; }
        public string Value { get; }
        public bool Result { get; }
        public RelayOffEventArgs(IInstrument relayType, int channelIndex, string value, bool result)
        {
            InstrumentType = relayType;
            ChannelIndex = channelIndex;
            Value = value;
            Result = result;
        }
    }

    public delegate void DAQCardOpenedEventHanlder(object sender, DAQCardOpenedEventArgs e);
    public class DAQCardOpenedEventArgs : EventArgs
    {
        public IInstrument InstrumentType { get; }
        public bool Result { get; }

        public DAQCardOpenedEventArgs(IInstrument instrumentType, bool result)
        {
            InstrumentType = instrumentType;
            Result = result;
        }
    }

    public delegate void DAQCardClosedEventHanlder(object sender, DAQCardClosedEventArgs e);
    public class DAQCardClosedEventArgs : EventArgs
    {
        public IInstrument InstrumentType { get; }
        public bool Result { get; }

        public DAQCardClosedEventArgs(IInstrument instrumentType, bool result)
        {
            InstrumentType = instrumentType;
            Result = result;
        }
    }


}
