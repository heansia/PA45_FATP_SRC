using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.VisualBasic;

namespace FCT.Models
{
    public class TestItem : ITestItem
    {
        private string _value = "null";

        private bool _result = false;

        public string Name { get; set; }

        public string UpperLimit { get; set; }

        public string LowerLimit { get; set; }

        public string Description { get; set; }

        public string Value
        {
            get { return _value; }

            set
            {
                _value = value;

                if (_value.ToLower().Contains("time") && _value.ToLower().Contains("out"))
                {
                    MessageBox.Show(
                        string.Format($"result of {this.Name} is TIME OUT, \r\n please check the printer is running well."),
                        "Nomad4_ATE",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                Tested?.Invoke(this, new TestedEventArgs(this));
            }
        }

        public bool Result
        {
            get { return _result; }

            set
            {
                _result = value;
                TestFinished?.Invoke(this, new TestFinishedEventArgs(this));
            }
        }

        public TimeSpan Duration
        {
            get;
            set;
        }

        public event TestedEventHandler Tested;

        //[Obsolete("this seems to be unuseless.")]
        public event TestFinishedEventHandler TestFinished;

        public TestItem(string name, string upperLimit, string lowerLimit, string description)
        {
            Name = name;
            UpperLimit = upperLimit;
            LowerLimit = lowerLimit;
            Description = description;
        }

        public bool IsValueInRange<T>() where T : struct, IComparable<T>
        {
            try
            {
                if (!Information.IsNumeric(UpperLimit)) { return false; }

                if (!Information.IsNumeric(LowerLimit)) { return false; }

                if (!Information.IsNumeric(Value)) { return false; }

                // 1. 将TestItem的UpperLimit/LowerLimit/Value转换为目标数值类型T
                T upper = (T)Convert.ChangeType(UpperLimit, typeof(T));
                T lower = (T)Convert.ChangeType(LowerLimit, typeof(T));
                T value = (T)Convert.ChangeType(Value, typeof(T));

                // 2. 范围判断（IComparable<T>的CompareTo方法）
                return value.CompareTo(lower) >= 0 && value.CompareTo(upper) <= 0;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, string.Format($"Item Name is {this.Name}, FUNC [ {MethodBase.GetCurrentMethod().Name} ]"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw e;
            }
        }

    }
}
