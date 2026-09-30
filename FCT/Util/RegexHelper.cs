using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FCT.Util
{
    public static class RegexHelper
    {
        // 正则表达式：严格匹配AA:BB:CC:DD:EE:FF格式（大写、冒号分隔、6段2位）
        private const string MacPattern = @"^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$";
        // 静态Regex实例（避免重复编译，提升性能）
        private static readonly Regex MacRegex = new Regex(MacPattern, RegexOptions.Compiled);

        /// <summary>
        /// 从长字符串中提取**第一个**符合AA:BB:CC:DD:EE:FF格式的MAC地址
        /// </summary>
        /// <param name="input">待匹配的长字符串</param>
        /// <returns>第一个符合的MAC地址，无匹配则返回null</returns>
        public static string GetFirstMacAddress(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // 按空白符/标点分割长字符串，遍历匹配（适配MAC在字符串中被分隔的场景）
            foreach (var part in Regex.Split(input, @"[\s,;:.，；。、]"))
            {
                if (MacRegex.IsMatch(part))
                {
                    return part;
                }
            }
            return null;
        }

        /// <summary>
        /// 从长字符串中提取**所有**符合AA:BB:CC:DD:EE:FF格式的MAC地址（去重）
        /// </summary>
        /// <param name="input">待匹配的长字符串</param>
        /// <returns>所有符合的MAC地址集合，无匹配则返回空集合</returns>
        public static List<string> GetAllMacAddresses(string input)
        {
            var macList = new List<string>();
            if (string.IsNullOrWhiteSpace(input))
                return macList;

            // 分割字符串并匹配，去重后返回
            var macSet = new HashSet<string>();
            foreach (var part in Regex.Split(input, @"[\s,;:.，；。、]"))
            {
                if (MacRegex.IsMatch(part))
                {
                    macSet.Add(part);
                }
            }
            macList.AddRange(macSet);
            return macList;
        }


        /// <summary>
        /// 通用字符串匹配函数
        /// </summary>
        /// <param name="input">原始输入字符串</param>
        /// <param name="strFlag">定位标记字符串（以此为分界点）</param>
        /// <param name="searchDirection">查找方向：true=向前(标记左侧)，false=向后(标记右侧)</param>
        /// <param name="regexPattern">正则表达式匹配内容</param>
        /// <returns>匹配到的第一个结果，无匹配返回null</returns>
        public static string StringMatch(string input, string strFlag, bool searchDirection, string regexPattern)
        {
            // 1. 空值校验
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(strFlag) || string.IsNullOrEmpty(regexPattern))
            {
                return null;
            }

            // 2. 查找定位标记的位置
            int flagIndex = input.IndexOf(strFlag);
            if (flagIndex == -1) // 未找到标记
            {
                return null;
            }

            // 3. 根据方向截取待匹配的子字符串
            string searchRange = string.Empty;
            if (searchDirection)
            {
                // 向前查找：标记 左侧 的所有内容
                searchRange = input.Substring(0, flagIndex);
            }
            else
            {
                // 向后查找：标记 右侧 的所有内容
                int startIndex = flagIndex + strFlag.Length;
                if (startIndex >= input.Length) return null;
                searchRange = input.Substring(startIndex);
            }

            // 4. 正则表达式匹配（忽略大小写，匹配第一个结果）
            Match match = Regex.Match(searchRange, regexPattern, RegexOptions.IgnoreCase);

            // 5. 返回结果：匹配成功返回值，失败返回null
            return match.Success ? match.Value : null;
        }

        public static string GetString(string input, string strFlag, bool searchDirection)
        {
            
            // 1. 空值校验
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(strFlag))
            {
                return null;
            }

            // 2. 查找定位标记的位置
            int flagIndex = input.IndexOf(strFlag);
            if (flagIndex == -1) // 未找到标记
            {
                return null;
            }

            // 3. 根据方向截取待匹配的子字符串
            string searchRange = string.Empty;
            if (searchDirection)
            {
                // 向前查找：标记 左侧 的所有内容
                searchRange = input.Substring(0, flagIndex);
            }
            else
            {
                // 向后查找：标记 右侧 的所有内容
                int startIndex = flagIndex + strFlag.Length;
                if (startIndex >= input.Length) return null;
                searchRange = input.Substring(startIndex);
            }

            return searchRange;
        }

    }
}
