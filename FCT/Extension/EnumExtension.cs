using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FCT.Models.Products;

namespace FCT.Extension
{
    public static class EnumExtension
    {
        /// <summary>
        /// 获取枚举的 Description 特性文本
        /// </summary>
        /// <param name="enumValue">枚举实例</param>
        /// <returns>Description字符串；没有特性则返回枚举名字符串</returns>
        public static string GetDescription(this Enum enumValue)
        {
            if (enumValue == null) return string.Empty;

            // 获取枚举字段信息
            FieldInfo fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            // 读取DescriptionAttribute特性
            DescriptionAttribute attr = fieldInfo.GetCustomAttribute<DescriptionAttribute>();

            return attr?.Description ?? enumValue.ToString();
        }

        /// <summary>
        /// 获取枚举的 Description 特性文本
        /// </summary>
        /// <param name="enumValue">枚举实例</param>
        /// <returns>Description字符串；没有特性则返回枚举名字符串</returns>
        public static RFID_CC GetRIFDRegionAttribute(this Enum enumValue)
        {
            if (enumValue == null) return RFID_CC.Error;

            // 获取枚举字段信息
            FieldInfo fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            // 读取DescriptionAttribute特性
            RIFDRegionAttribute attr = fieldInfo.GetCustomAttribute<RIFDRegionAttribute>();

            return attr.CC;
        }
    }
}
