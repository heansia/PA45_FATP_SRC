using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT
{
    // 定义一个特性，专门用来标记测试方法
    [AttributeUsage(AttributeTargets.Method)]
    public class QBC_Test_ImplementationAttribute : Attribute
    {
        // 这里可以加属性，也可以什么都不写，只是作为标记
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class Printer_Test_ImplementationAttribute : Attribute
    {
        
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class USB2ETH_Test_ImplementationAttribute : Attribute
    {
        
    }


    public class argumentsAttribute : Attribute
    {
        public argumentsAttribute(string arg)
        {

        }
    }
}
