using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FCT.Extension;
using FCT.Models.Products.Interface;

namespace FCT.Models.Products
{
    public class PA45 : Product
    {
        private static readonly Dictionary<string, CommunicationOptions> CommOptionSkuMap;

        static PA45()
        {
            CommOptionSkuMap = new Dictionary<string, CommunicationOptions>();
            CommOptionSkuMap.Add("00", CommunicationOptions._00);
            CommOptionSkuMap.Add("B1", CommunicationOptions._B1);
            CommOptionSkuMap.Add("D0", CommunicationOptions._D0);
            CommOptionSkuMap.Add("E0", CommunicationOptions._E0);
            CommOptionSkuMap.Add("F1", CommunicationOptions._F1);
            CommOptionSkuMap.Add("F2", CommunicationOptions._F2);
            CommOptionSkuMap.Add("FX", CommunicationOptions._FX);
            CommOptionSkuMap.Add("G1", CommunicationOptions._G1);
            CommOptionSkuMap.Add("G2", CommunicationOptions._G2);
        }

        private static CommHWOpt _commHWOpt;
        public static CommHWOpt CommHWOpt
        {
            get
            {
                return _commHWOpt;
            }
        }

        public Version Version
        {
            get
            {
                string m_str = HON_PN.Substring(4, 1).ToUpper();

                Version _version = Version.Unknown;
                if (m_str == "S")
                {
                    _version = Version.Small;
                }

                if (m_str == "L")
                {
                    _version = Version.Large;
                }

                string msg = string.Format($"detected Version is {_version}.");
                OnRunTimeLogUpdated(msg);

                return _version;
            }
        }

        public SKU Sku
        {
            get
            {
                string m_str = HON_PN.Substring(6, 2).ToUpper();
                SKU _sku = SKU.Unknown;
                if (m_str == "00") { _sku = SKU.Standard; }
                if (m_str == "FD") { _sku = SKU.FamilyDollar; }
                if (m_str == "FE") { _sku = SKU.FedEx; }
                if (m_str == "B0") { _sku = SKU.Bimbo; }
                if (m_str == "VA") { _sku = SKU.Vanguard; }

                string msg = string.Format($"detected SKU is {_sku}.");
                OnRunTimeLogUpdated(msg);

                return _sku;
            }
        }

        public RegionalCode RegionalCode
        {
            get
            {
                string m_str = HON_PN.Substring(7, 1).ToUpper();
                RegionalCode _regionalCode = RegionalCode.Unknown;
                if (m_str == "0") { _regionalCode = RegionalCode.Global; }
                if (m_str == "N") { _regionalCode = RegionalCode.Mexico; }
                if (m_str == "9") { _regionalCode = RegionalCode.China; }

                string msg = string.Format($"detected Regional Code is {_regionalCode}.");
                OnRunTimeLogUpdated(msg);
                return _regionalCode;
            }
        }

        public RegionalCodeFlag RegionalCodeFlag
        {
            get
            {
                string m_str = HON_PN.Substring(7, 1).ToUpper();
                RegionalCodeFlag _regionalCodeStr = RegionalCodeFlag.Unknown;

                if (RegionalCode == RegionalCode.Unknown)
                {
                    _regionalCodeStr = RegionalCodeFlag.Unknown;
                    goto End;
                }

                if (RegionalCode.ToString().ToUpper().Contains("ROW"))
                {
                    _regionalCodeStr = RegionalCodeFlag.ROW;
                }
                else if (RegionalCode.ToString().ToUpper().Contains("US"))
                {
                    _regionalCodeStr = RegionalCodeFlag.US;
                }
                else if (RegionalCode.ToString().ToUpper().Contains("EU"))
                {
                    _regionalCodeStr = RegionalCodeFlag.EU;
                }
                else
                {
                    _regionalCodeStr = RegionalCodeFlag.ROW;
                }

            End:
                string msg = string.Format($"detected Regional Code is {_regionalCodeStr}.");
                OnRunTimeLogUpdated(msg);
                return _regionalCodeStr;
            }
        }

        public MediaHandling MediaHandling
        {
            get
            {
                string m_str = HON_PN.Substring(8, 1).ToUpper();

                MediaHandling _mediaHandling = MediaHandling.Unknown;
                if (m_str == "0") { _mediaHandling = MediaHandling.StandardPlatenRoller; }
                if (m_str == "1") { _mediaHandling = MediaHandling.LinerlessPlatenRoller; }
                if (m_str == "2") { _mediaHandling = MediaHandling.Peeler; }

                string msg = string.Format($"detected MediaHandling config is {_mediaHandling}.");
                OnRunTimeLogUpdated(msg);

                return _mediaHandling;
            }
        }

        public CommunicationOptions CommunicationOption
        {
            get
            {
                string m_str = HON_PN.Substring(9, 2).ToUpper();
                CommunicationOptions commOpt = CommunicationOptions.Unknown;

                string str_flag = "Unknown";

                bool isFound = CommOptionSkuMap.TryGetValue(m_str, out commOpt);

                if (isFound) { str_flag = commOpt.GetDescription(); }


                if (str_flag.ToLower().Contains("bt") && str_flag.ToLower().Contains("wi-fi") && str_flag.ToLower().Contains("rfid"))
                {
                    _commHWOpt = CommHWOpt.WIFI_BLUETOOTH_RFID;
                }
                else if (str_flag.ToLower().Contains("bt") && str_flag.ToLower().Contains("wi-fi") && !str_flag.ToLower().Contains("rfid"))
                {
                    _commHWOpt = CommHWOpt.WIFI_BLUETOOTH;
                }
                else if (str_flag.ToLower().Contains("bt") && str_flag.ToLower().Contains("only") && str_flag.ToLower().Contains("usb"))
                {
                    _commHWOpt = CommHWOpt.BLUETOOTH_ONLY;
                }
                else if (str_flag.ToLower().Contains("bt") && str_flag.ToLower().Contains("only") && !str_flag.ToLower().Contains("usb"))
                {
                    _commHWOpt = CommHWOpt.USB_ONLY;
                }
                else
                {
                    _commHWOpt = CommHWOpt.ERROR;
                }

                string msg = string.Format($"detected CommunicationOption config is {str_flag} ({commOpt}).");
                OnRunTimeLogUpdated(msg);
                return commOpt;
            }
        }

        public BatteryOptions BatteryOption
        {
            get
            {
                string m_str = HON_PN.Substring(11, 1).ToUpper();
                BatteryOptions _BatteryOption = BatteryOptions.Unknown;

                if (m_str == "E")
                {
                    _BatteryOption = BatteryOptions.NoBattery;
                }
                if (m_str == "1")
                {
                    _BatteryOption = BatteryOptions.Strandard;
                }
                if (m_str == "2")
                {
                    _BatteryOption = BatteryOptions.Extended;
                }
                if (m_str == "D")
                {
                    _BatteryOption = BatteryOptions.DisinfectantReadyHousing;
                }


                string msg = string.Format($"detected BatteryOption config is {_BatteryOption}.");
                OnRunTimeLogUpdated(msg);
                return _BatteryOption;
            }
        }

        public string HON_PN { get; set; }

        public PA45() : base()
        {

        }


        public PA45(string sn, IProductConfig productConfig)
            : base(sn, productConfig)
        {

        }

        public override bool CheckBarcodeFormat()
        {
            if (!CheckPNStartWith() || !CheckPNLength()) { return false; }

            if (Version == Version.Unknown) { return false; }

            if (Sku == SKU.Unknown) { return false; }

            if (RegionalCode == RegionalCode.Unknown) { return false; }

            if (CommunicationOption == CommunicationOptions.Unknown) { return false; }

            if (BatteryOption == BatteryOptions.Unknown) { return false; }

            if (RegionalCodeFlag == RegionalCodeFlag.Unknown) { return false; }

            return base.CheckBarcodeFormat();
        }

        private bool CheckPNStartWith()
        {
            bool rlt = HON_PN.StartsWith(ProductConfig.PN_START_WITH);
            string msg = string.Format($"{SerialNumber} FUNC [ {MethodBase.GetCurrentMethod().Name} ] {rlt}");
            OnRunTimeLogUpdated(msg);
            return rlt;
        }

        private bool CheckPNLength()
        {
            bool rlt = HON_PN.Length == Convert.ToInt32(ProductConfig.PN_LENGTH);
            string msg = string.Format($"{SerialNumber} FUNC [ {MethodBase.GetCurrentMethod().Name} ] {rlt}");
            OnRunTimeLogUpdated(msg);
            return rlt;
        }


    }

    public enum Version
    {
        Unknown,
        Small,
        Large
    }

    public enum SKU
    {
        /*
         Standard/ Custom SKU (Digits 6,7)
         00 = Standard version
         FD = Family Dollar
         FE = FedEx
         B0=Bimbo
         VA = Vanguard
         */
        Unknown,
        Standard,
        FamilyDollar,
        FedEx,
        Bimbo,
        Vanguard
    }

    public enum RegionalCode
    {
        /*
         Regional Code (Digit 8)
         0 = Global
         N = Mexico
         9= reserve for China
         */

        Unknown,
        Global,
        Mexico,
        China
    }

    public enum RegionalCodeFlag
    {
        /*
         Regional Code (Digit 8)
         0 = Global
         N = Mexico
         9= reserve for China
         */

        Unknown,
        ROW,
        US,
        EU
    }

    public enum MediaHandling
    {
        /*(Digit 9)
         * 0 = Standard Platen Roller
         * 1 = Linerless Platen Roller
         * 2 = Peeler
         */

        Unknown,
        StandardPlatenRoller,
        LinerlessPlatenRoller,
        Peeler
    }

    public enum CommunicationOptions
    {
        Unknown,

        [Description("USB only")]
        _00,

        [Description("BT Only without USB-C cable")]
        _B1,

        [Description("BT & Wi-Fi Global without USB-C cable")]
        _D0,

        [Description("BT & Wi-Fi Global & WWAN Global without USB-C cable")]
        _E0,

        [RIFDRegionAttribute(RFID_CC.NA)]
        [Description("BT & Wi-Fi Global & WWAN & RFID US without USB-C cable")]
        _F1,

        [RIFDRegionAttribute(RFID_CC.EU)]
        [Description("BT & Wi-Fi Global & WWAN & RFID EU without USB-C cable")]
        _F2,

        [Description("BT & Wi-Fi Global WWAN & RFID ROW (other RFID SKUs to be set based on regional demand) without USB-C cable")]
        _FX,

        [RIFDRegionAttribute(RFID_CC.NA)]
        [Description("BT & Wi-Fi Global & RFID US")]
        _G1,

        [RIFDRegionAttribute(RFID_CC.EU)]
        [Description("BT & Wi-Fi Global & RFID EU")]
        _G2

    }

    public enum BatteryOptions
    {
        /*
         * E = No Battery
         * 1 = Strandard, 2 cell battery
         * 2 = Extended,  4 cell battery
         * D = Disinfectant Ready Housing
         */

        Unknown,
        NoBattery,
        Strandard,
        Extended,
        DisinfectantReadyHousing
    }

    public enum RFID_Country_Code
    {
        Unknown,
        NONE,
        US
    }

    public enum RFID_Country_Name
    {
        Europe = 2,
        NorthAmerica = 14,
        Thailand = 19,
        Malaysia = 28,
        Vietnam = 32,
        China = 34,
        SouthKorea = 38,
        Taiwan = 16,
        Australia = 20,
        NewZealand = 20,
        Japan = 27,
        HongKong = 26,
        Singapore = 30,
        Indonesia = 33,
        Philipine = 35
    }

    public enum RFID_CC
    {
        Error,

        [Description("02")]
        EU,

        [Description("14")]
        NA,

        [Description("19")]
        TH,

        [Description("28")]
        MY,

        [Description("32")]
        VN,

        [Description("34")]
        CN,

        [Description("38")]
        KR,

        [Description("16")]
        TW,

        [Description("20")]
        AZ,

        [Description("27")]
        JP,

        [Description("26")]
        HK,

        [Description("30")]
        SG,

        [Description("33")]
        ID,

        [Description("35")]
        PH
    }

    public enum CommHWOpt
    {
        ERROR,
        USB_ONLY,
        BLUETOOTH_ONLY,
        WIFI_BLUETOOTH,
        WIFI_BLUETOOTH_RFID
    }


    // 自定义Attribute，支持绑定RegionCode，还可以保留描述
    [AttributeUsage(AttributeTargets.Field)]
    public class RIFDRegionAttribute : Attribute
    {
        
        public RFID_CC CC { get; }

        public RIFDRegionAttribute(RFID_CC cc)
        {
            CC = cc;
        }

    }
}


