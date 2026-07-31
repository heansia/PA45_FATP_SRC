using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using FCT.Models.DUT.Interface;
using INI = FCT.Utilities.IniFileHelper;

namespace FCT.Models
{
    public class Product : IProduct, IDisposable
    {
        public static readonly DataTable ProductMatrix = Global.PRODUCT_SETTING_MATRIX;

        public string SerialNumber { get; set; }

        public IProductConfig ProductConfig { get; }

        /*
        public Product(string sn) : base(sn)
        {
            SerialNumber = sn;
        }
        */

        public Product(string sn, IProductConfig productConfig)
        {
            SerialNumber = sn;
        }

        public Product()
        {

        }

        public bool CheckBarcodeFormat()
        {
            string configFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + "Config.ini";

            string startWith = INI.GetValue("SN_Format", "StartWith", configFile);
            string length = INI.GetValue("SN_Format", "Length", configFile);

            bool rlt = false;
            string message;

            if (SerialNumber.Length != Convert.ToInt32(length))
            {
                message = SerialNumber + " CheckBarcodeFormat() result is " + rlt.ToString() + "." + " Length error.";
                MessageBox.Show(message, "SN format check", MessageBoxButtons.OK, MessageBoxIcon.Error);

                goto End;
            }

            if (!SerialNumber.StartsWith(startWith))
            {
                message = SerialNumber + " CheckBarcodeFormat() result is " + rlt.ToString() + "." + " StartWith error.";
                MessageBox.Show(message, "SN format check", MessageBoxButtons.OK, MessageBoxIcon.Error);

                goto End;
            }

            rlt = true;
            message = string.Format($"{SerialNumber} FUNC [ {MethodBase.GetCurrentMethod().Name} ] OK");

        End:
            RunTimeLogUpdated?.Invoke(this, new RunTimeLogUpdatedEventArgs(message));
            return rlt;
        }

        public bool CheckBarcodeFormat1()
        {
            return CheckLength() && CheckStartWith();
        }

        private bool CheckLength()
        {
            bool rlt = SerialNumber.Length == Convert.ToInt32(ProductConfig.SN_LENGTH);

            if (!rlt)
            {
                MessageBox.Show(
                    string.Format($"{SerialNumber}'s length should be {ProductConfig.SN_LENGTH}"),
                    MethodBase.GetCurrentMethod().Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            string message = string.Format($"{SerialNumber} FUNC [ {MethodBase.GetCurrentMethod().Name} ] {rlt}");
            RunTimeLogUpdated?.Invoke(this, new RunTimeLogUpdatedEventArgs(message));
            return rlt;
        }

        private bool CheckStartWith()
        {
            bool rlt = SerialNumber.StartsWith(ProductConfig.SN_START_WITH);

            if (!rlt)
            {
                MessageBox.Show(
                    string.Format($"{SerialNumber} should start with {ProductConfig.SN_START_WITH}"),
                    MethodBase.GetCurrentMethod().Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            string message = string.Format($"{SerialNumber} FUNC [ {MethodBase.GetCurrentMethod().Name} ] {rlt}");
            RunTimeLogUpdated?.Invoke(this, new RunTimeLogUpdatedEventArgs(message));
            return rlt;
        }

        public event RunTimeLogUpdatedEventHanlder RunTimeLogUpdated;

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~Product() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            RunTimeLogUpdated = null;
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }




}
