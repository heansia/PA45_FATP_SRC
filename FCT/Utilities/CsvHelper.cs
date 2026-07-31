using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Utilities
{
    public static class CsvHelper
    {

        /// <summary>
        /// CSV文件转DataTable
        /// </summary>
        /// <param name="filePath">CSV文件路径</param>
        /// <param name="encoding">编码，默认UTF8</param>
        /// <returns></returns>
        public static DataTable CsvToDataTable(string filePath, Encoding encoding = null)
        {
            if (!File.Exists(filePath)) { throw new FileNotFoundException("文件不存在", filePath); }
            if (encoding == null) { encoding = Encoding.UTF8; }

            DataTable dt = new DataTable();
            string[] lines = File.ReadAllLines(filePath, encoding);

            if (lines.Length == 0) return dt;

            // 解析表头
            string[] headers = SplitCsvLine(lines[0]);
            foreach (var col in headers) { dt.Columns.Add(col.Trim()); }

            // 解析数据行
            for (int i = 1; i < lines.Length; i++)
            {
                string[] rowData = SplitCsvLine(lines[i]);
                DataRow row = dt.NewRow();
                for (int j = 0; j < rowData.Length && j < dt.Columns.Count; j++)
                {
                    row[j] = rowData[j].Trim();
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        // 处理带引号、逗号内嵌的CSV行分割
        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuote = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuote = !inQuote;
                }
                else if (c == ',' && !inQuote)
                {
                    result.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }
            result.Add(field.ToString());
            return result.ToArray();
        }

    }
}
