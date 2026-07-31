using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models
{
    public interface ITestItem
    {
        string Name { get; set; }
        string UpperLimit { get; set; }
        string LowerLimit { get; set; }
        string Description { get; set; }
        string Value { get; set; }
        bool Result { get; set; }
        TimeSpan Duration { get; set; }
        bool IsValueInRange<T>() where T : struct, IComparable<T>;
    }
}
