using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.Products.Interface
{
    public interface IProduct
    {
        event RunTimeLogUpdatedEventHanlder RunTimeLogUpdated;
        
        string SerialNumber { get; }

        IProductConfig ProductConfig { get; }

        bool CheckBarcodeFormat();

        void Dispose();

        void OnRunTimeLogUpdated(string message, bool isAddTimestamp = false);
    }
}
