using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.Products.Interface
{
    public abstract class ProductBase
    {
        public ProductBase(string serialNumber)
        {
            SerialNumber = serialNumber;

            if (string.IsNullOrEmpty(SerialNumber))
                throw new Exception("序列号不能为空");
        }

        public string SerialNumber { get; }

        public string ModelName { get; }

        public string ModelNumber { get; }

        public abstract bool CheckBarcodeFormat();

        public void Dispose()
        {

        }

        public event RunTimeLogUpdatedEventHanlder RunTimeLogUpdated;
    }
}
