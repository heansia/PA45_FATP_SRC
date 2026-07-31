using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.Products
{
    class WiFi_BT_Module : Product
    {
        public WiFi_BT_Module(string sn, IProductConfig productConfig) : base(sn, productConfig) { }

        public WiFi_BT_Module() : base() { }
        
        public Vendor Vendor
        {
            get
            {
                Vendor m_Vendor = Vendor.UnKnown;

                if (SerialNumber.Substring(0, 1).ToUpper() == "P") { m_Vendor = Vendor.Primax; }
                OnRunTimeLogUpdated($"Detect this board Vendor is {m_Vendor}.");

                return m_Vendor;
            }
        }

        public MadeIn MadeIn
        {
            get
            {
                MadeIn m_MadeIn = MadeIn.UnKnown;

                if (SerialNumber.Substring(1, 1).ToUpper() == "C") { m_MadeIn = MadeIn.China; }
                else if (SerialNumber.Substring(1, 1).ToUpper() == "T") { m_MadeIn = MadeIn.Thailand; }
                else { m_MadeIn = MadeIn.UnKnown; }

                OnRunTimeLogUpdated($"Detect this board is made in {m_MadeIn}.");

                return m_MadeIn;
            }
        }

        public Project Project
        {
            get
            {
                Project m_proj = Project.UnKnown;

                if (SerialNumber.Substring(2, 1).ToUpper() == "N") { m_proj = Project.Nomad4; }

                OnRunTimeLogUpdated($"Detect this board is in {m_proj} Project.");
                return m_proj;
            }
        }

        public Model Model
        {
            get
            {
                Model m_model = Model.UnKnown;

                if (SerialNumber.Substring(3, 2).ToUpper() == "01") { m_model = Model.MainBoard; }
                if (SerialNumber.Substring(3, 2).ToUpper() == "02") { m_model = Model.PowerBoard; }
                if (SerialNumber.Substring(3, 2).ToUpper() == "03") { m_model = Model.WiFi_BT_Module; }
                if (SerialNumber.Substring(3, 2).ToUpper() == "04") { m_model = Model.BT_Module; }
                OnRunTimeLogUpdated($"Detect this board Model is {m_model} .");

                return m_model;
            }
        }
        
        public override bool CheckBarcodeFormat()
        {

            if (!base.CheckBarcodeFormat()) { return false; }

            if (Vendor == Vendor.UnKnown) { return false; }

            if (MadeIn == MadeIn.UnKnown) { return false; }

            if (Project == Project.UnKnown) { return false; }

            if (Model != Model.WiFi_BT_Module) { return false; }

            string m_year = SerialNumber.Substring(5, 2);
            OnRunTimeLogUpdated($"MFG year is 20{m_year} .");

            string m_month = SerialNumber.Substring(7, 1);
            OnRunTimeLogUpdated($"MFG month is {Convert.ToDecimal(m_month)} .");

            string m_day = SerialNumber.Substring(8, 2);
            OnRunTimeLogUpdated($"MFG day is {Convert.ToDecimal(m_day)} .");

            string m_sn = SerialNumber.Substring(10, 4);
            OnRunTimeLogUpdated($"MFG serial number is {Convert.ToDecimal(m_sn)} .");

            return true;
        }


    }
}
