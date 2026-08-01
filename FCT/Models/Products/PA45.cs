using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FCT.Models.Products.Interface;

namespace FCT.Models.Products
{
    public class PA45 : Product
    {
        public Version Version
        {
            get
            {
                string m_str = HON_PN.Substring(4, 1).ToUpper();

                Version _version = Version.Unknown;
                if (m_str == "S") { _version = Version.Small; }
                if (m_str == "L") { _version = Version.Large; }

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

                if (RegionalCode.ToString().ToUpper().Contains("ROW")) { _regionalCodeStr = RegionalCodeFlag.ROW; }
                else if (RegionalCode.ToString().ToUpper().Contains("US")) { _regionalCodeStr = RegionalCodeFlag.US; }
                else if (RegionalCode.ToString().ToUpper().Contains("EU")) { _regionalCodeStr = RegionalCodeFlag.EU; }
                else { _regionalCodeStr = RegionalCodeFlag.ROW; }

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
                CommunicationOptions _communicationOption = CommunicationOptions.Unknown;

                string str_flag = "Unknown";
                if (m_str == "00")
                {
                    //00 = USB only
                    _communicationOption = CommunicationOptions.USB_only;
                    str_flag = "USB only";
                }
                if (m_str == "B1")
                {
                    //B1 = BT Only
                    _communicationOption = CommunicationOptions.BT_only;
                    str_flag = "BT Only";
                }
                if (m_str == "D1")
                {
                    //D1 = BT & Wi-Fi US
                    _communicationOption = CommunicationOptions.BT_WifiUS;
                    str_flag = "BT & Wi-Fi US";
                }
                if (m_str == "D2")
                {
                    //D2 = BT & Wi-Fi ROW
                    _communicationOption = CommunicationOptions.BT_WifiROW;
                    str_flag = "BT & Wi-Fi ROW";
                }
                if (m_str == "E1")
                {
                    //E1 = BT & Wi-Fi US & WWAN US
                    _communicationOption = CommunicationOptions.BT_WifiUS_WwanUS;
                    str_flag = "BT & Wi-Fi US & WWAN US";
                }
                if (m_str == "E2")
                {
                    //E2 = BT & Wi-Fi ROW & WWAN EU
                    _communicationOption = CommunicationOptions.BT_WifiROW_WwanEU;
                    str_flag = "BT & Wi-Fi ROW & WWAN EU";
                }
                if (m_str == "Ex")
                {
                    //Ex = BT & Wi-Fi ROW & WWAN ROW
                    _communicationOption = CommunicationOptions.BT_WifiROW_WwanROW;
                    str_flag = "BT & Wi-Fi ROW";
                }
                if (m_str == "F1")
                {
                    //F1 = BT & Wi-Fi US & WWAN & RFID US
                    _communicationOption = CommunicationOptions.BT_WifiUS_Wwan_RfidUS;
                }
                if (m_str == "F2")
                {
                    //F2 = BT & Wi-Fi ROW & WWAN & RFID EU
                    _communicationOption = CommunicationOptions.USB_only;
                }
                if (m_str == "Fx")
                {
                    //Fx = BT & Wi - Fi & WWAN & RFID ROW(other RFID SKUs to be set based on regional demand)
                    _communicationOption = CommunicationOptions.USB_only;
                }


                string msg = string.Format($"detected CommunicationOption config is {str_flag} ({_communicationOption}).");
                OnRunTimeLogUpdated(msg);
                return _communicationOption;
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
        /*
         * 00 = USB only
         * B1 = BT Only
         * D1 = BT & Wi-Fi US
         * D2 = BT & Wi-Fi ROW
         * E1 = BT & Wi-Fi US & WWAN US.
         * E2 = BT & Wi-Fi ROW & WWAN EU
         * Ex = BT & Wi-Fi ROW & WWAN ROW
         * F1 = BT & Wi-Fi US & WWAN & RFID US
         * F2 = BT & Wi-Fi ROW & WWAN & RFID EU
         * Fx = BT & Wi-Fi & WWAN & RFID ROW (other RFID SKUs to be set based on regional demand)
         */

        Unknown,
        USB_only,
        BT_only,
        BT_WifiUS,
        BT_WifiROW,
        BT_WifiUS_WwanUS,
        BT_WifiROW_WwanEU,
        BT_WifiROW_WwanROW,
        BT_WifiUS_Wwan_RfidUS,
        BT_WifiROW_Wwan_RfidEU,
        BT_Wifi_Wwan_RfidROW
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

}
