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
using FCT.Models.Products.Interface;
using INI = FCT.Utilities.INI_Parser;

namespace FCT.Models.Products
{
    public class Product : IProduct, IDisposable
    {
        public static readonly DataTable ProductMatrix = Global.PRODUCT_SETTING_MATRIX;

        public string SerialNumber { get; set; }

        public IProductConfig ProductConfig { get; }

        public virtual void OnRunTimeLogUpdated(string message, bool isAddTimestamp = false)
        {
            // 触发事件（?. 防止没有订阅者时报错）
            RunTimeLogUpdated?.Invoke(this, new RunTimeLogUpdatedEventArgs(message, isAddTimestamp));
        }

        /*
        public Product(string sn) : base(sn)
        {
            SerialNumber = sn;
        }
        */

        public Product(string sn, IProductConfig productConfig)
        {
            SerialNumber = sn;
            ProductConfig = productConfig;
        }
        
        public Product() { }

        public virtual bool CheckBarcodeFormat() { return CheckLength() && CheckStartWith(); }

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
            OnRunTimeLogUpdated(message);
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
            OnRunTimeLogUpdated(message);
            return rlt;
        }

        public event RunTimeLogUpdatedEventHanlder RunTimeLogUpdated;

        // 根据 product 动态创建对应的 DUT 子类（QBC、ABC、XYZ...）
        public static Product CreateProductByProdConfig(string SeriNumber, IProductConfig iProdConfig)
        {
            try
            {
                // 1. 从 product 里拿到 类名（你自己决定用哪个字段）
                string className = iProdConfig.MODEL; // 例如："QBC"

                // 2. 拼接完整类名（必须写对命名空间！）
                string fullClassName = $"FCT.Models.Products.{className}";
                // 效果：FCT.Models.DUT.QBC.QBC

                // 3. 根据字符串找到类的 Type
                Type type = Type.GetType(fullClassName);

                if (type == null)
                {
                    // 找不到类，抛异常
                    throw new Exception($"找不到设备类型：{fullClassName}");
                }

                // 4. 动态创建对象，自动调用构造函数：new QBC(product)
                object instance = Activator.CreateInstance(type, SeriNumber, iProdConfig);

                // 5. 强转成接口 IDUT
                Product product = instance as Product;

                if (product == null)
                {
                    throw new Exception($"{className} 没有实现 Product 接口");
                }

                return product;
            }
            catch (Exception ex)
            {
                throw new Exception("动态创建Product失败：" + ex.Message);
            }
        }


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
