using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using FCT.Models.DUT.Interface;
using FCT.Models.Products.Interface;
using INI = FCT.Utilities.IniFileHelper;

namespace FCT.Models.DUT
{
    public class DUT : IDUT
    {
        private Test_Proc_Error_Code _errorCode;
        
        public virtual string PID { get { return string.Empty; } }
        public virtual string VID { get { return string.Empty; } }
        public string SerialNumber
        {
            get
            {
                return Product.SerialNumber;
            }
        }
        public IProduct Product { get; set; }
        public Test_Proc_Error_Code ErrorCode
        {
            get
            {
                return _errorCode;
            }
            set
            {
                _errorCode = value;
                TestErrorStatusChanged?.Invoke(this, new TestErrorStatusChangedEventArgs(_errorCode));
            }
        }
        public bool FinalResult
        {
            get
            {
                bool _rlt = false;
                for (int i = 0; i < TestItems.Count; i++)
                {
                    _rlt = TestItems[i].Result;
                    if (!_rlt) { break; }
                }
                return _rlt;
            }
        }
        public List<TestItem> TestItems { get; } = new List<TestItem>();

        public Func<int, TestItem, TestItem> TestMethod { get; set; }

        public DUT(IProduct product)
        {
            Product = product;
        }

        public virtual void InitializeTestItem()
        {
            string configFile = Path.Combine(Global.ASSEMBLY_PATH, this.Product.ProductConfig.TEST_ITEMS);

            TestItems.Clear();

            int count = Convert.ToInt32(INI.GetValue("TestCount", "Count", configFile));

            for (int i = 1; i < count + 1; i++)
            {
                if (Convert.ToBoolean(INI.GetValue(i.ToString(), "TestEnable", configFile)))
                {
                    string name = INI.GetValue(i.ToString(), "TestName", configFile);
                    string upperLimit = INI.GetValue(i.ToString(), "TestUpperLimit", configFile);
                    string lowerLimit = INI.GetValue(i.ToString(), "TestLowerLimit", configFile);
                    string description = INI.GetValue(i.ToString(), "Description", configFile);

                    TestItem item = new TestItem(name, upperLimit, lowerLimit, description);

                    TestItems.Add(item);
                }
            }

            string message = string.Format($"{SerialNumber} FUNC [ {MethodBase.GetCurrentMethod().Name} ] OK.");

            RunTimeLogUpdated?.Invoke(this, new RunTimeLogUpdatedEventArgs(message));
        }
        public virtual void Test()
        {
            if (TestMethod != null)
            {

            }
        }
        public event RunTimeLogUpdatedEventHanlder RunTimeLogUpdated;
        public event TestErrorStatusChangedEventHandler TestErrorStatusChanged;

        // 根据 product 动态创建对应的 DUT 子类（QBC、ABC、XYZ...）
        public static IDUT CreateDutByProduct(IProduct product)
        {
            try
            {
                // 1. 从 product 里拿到 类名（你自己决定用哪个字段）
                string className = product.ProductConfig.MODEL; // 例如："QBC"

                // 2. 拼接完整类名（必须写对命名空间！）
                string fullClassName = $"FCT.Models.DUT.{className}.{className}";
                // 效果：FCT.Models.DUT.QBC.QBC

                // 3. 根据字符串找到类的 Type
                Type type = Type.GetType(fullClassName);

                if (type == null)
                {
                    // 找不到类，抛异常
                    throw new Exception($"找不到设备类型：{fullClassName}");
                }

                // 4. 动态创建对象，自动调用构造函数：new QBC(product)
                object instance = Activator.CreateInstance(type, product);

                // 5. 强转成接口 IDUT
                IDUT dut = instance as IDUT;

                if (dut == null)
                {
                    throw new Exception($"{className} 没有实现 IDUT 接口");
                }

                return dut;
            }
            catch (Exception ex)
            {
                throw new Exception("动态创建DUT失败：" + ex.Message);
            }
        }
    }
}
