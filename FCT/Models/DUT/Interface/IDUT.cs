using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products.Interface;

namespace FCT.Models.DUT.Interface
{
    public interface IDUT
    {
        /*
         * DUT is a product carrying test items.
         * Product has no logic func for testing.
         */

        string SerialNumber { get; }

        IProduct Product { get; set; }

        Test_Proc_Error_Code ErrorCode { get; set; }

        bool FinalResult { get; }

        List<TestItem> TestItems { get; }

        void InitializeTestItem();

        void Test();

        Func<int, TestItem, TestItem> TestMethod { get; set; }
        
        event RunTimeLogUpdatedEventHanlder RunTimeLogUpdated;

        event TestErrorStatusChangedEventHandler TestErrorStatusChanged;

        string PID { get; }
    }
}
