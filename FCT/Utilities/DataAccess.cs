using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using FCT.Models;
using FCT.Models.DUT.Interface;

namespace FCT.Utilities
{
    public static class DataAccess
    {
        public static void SaveLog(string value, string path, bool append = true)
        {
            using (StreamWriter writer = new StreamWriter(path, append))
            {
                writer.WriteLine(value);
            }
        }

        public static void SaveCsvLog(IDUT dut, string mbPartNumber)
        {
            string folder = Global.ASSEMBLY_PATH + @"\" + "CsvLog";

            Type m_type = dut.GetType();

            folder = folder + @"\" + m_type.ToString();

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            folder = folder + @"\" + dut.SerialNumber;

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string file = folder + @"\" + dut.SerialNumber + ".csv";

            bool isHeaderThere = File.Exists(file);

            using (StreamWriter sw = new StreamWriter(file, true))
            {
                StringBuilder sb = new StringBuilder();
                if (!isHeaderThere)
                {
                    string header = "Barcode" + "," + "PART_NUMBER" + "," + "TestTime" + ",";
                    for (int i = 0; i < dut.TestItems.Count(); i++)
                    {
                        header = header + dut.TestItems[i].Name + ",";
                    }
                    sb.AppendLine(header);

                    string value = dut.SerialNumber + "," + mbPartNumber + "," + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ",";

                    for (int i = 0; i < dut.TestItems.Count(); i++)
                    {
                        value = value + dut.TestItems[i].Value.Replace("\r\n", "").Replace("\n", "").Replace("\r", "") + ",";
                    }
                    sb.AppendLine(value);
                }
                else
                {
                    string value = dut.SerialNumber + "," + mbPartNumber + "," + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ",";

                    for (int i = 0; i < dut.TestItems.Count(); i++)
                    {
                        value = value + dut.TestItems[i].Value.Replace("\r\n", "").Replace("\n", "").Replace("\r", "") + ",";
                    }
                    sb.AppendLine(value);
                }

                sw.Write(sb);

            }

        }

        public static void SaveCsvLogByDay(IDUT dut, string mbPartNumber)
        {
            string folder = Global.ASSEMBLY_PATH + @"\" + "CsvLog";

            Type m_type = dut.GetType();

            folder = folder + @"\" + m_type.ToString();

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            folder = folder + @"\" + DateTime.Now.ToString("yyyy-MM");

            folder = folder + @"\" + DateTime.Now.Day.ToString();

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string file = folder + @"\" + DateTime.Now.ToString("yyyy_MM_dd") + ".csv";

            bool isHeaderThere = File.Exists(file);

            using (StreamWriter sw = new StreamWriter(file, true))
            {
                StringBuilder sb = new StringBuilder();
                if (!isHeaderThere)
                {
                    string header = "Barcode" + "," + "PART_NUMBER" + "," + "TestTime" + ",";
                    for (int i = 0; i < dut.TestItems.Count(); i++)
                    {
                        header = header + dut.TestItems[i].Name + ",";
                    }
                    sb.AppendLine(header);

                    string value = dut.SerialNumber + "," + mbPartNumber + "," + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ",";

                    for (int i = 0; i < dut.TestItems.Count(); i++)
                    {
                        value = value + dut.TestItems[i].Value.Replace("\r\n", "").Replace("\n", "").Replace("\r", "") + ",";
                    }
                    sb.AppendLine(value);
                }
                else
                {
                    string value = dut.SerialNumber + "," + mbPartNumber + "," + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ",";

                    for (int i = 0; i < dut.TestItems.Count(); i++)
                    {
                        value = value + dut.TestItems[i].Value.Replace("\r\n", "").Replace("\n", "").Replace("\r", "") + ",";
                    }
                    sb.AppendLine(value);
                }

                sw.Write(sb);

            }

        }



        public static void SaveTxtLog(IDUT dut, RichTextBox rtbLog, RichTextBox rtbUSB, RichTextBox rtbSerialport)
        {
            string folder = Application.StartupPath + @"\" + "TxtLog";

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            folder = folder + @"\" + dut.SerialNumber;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string file = folder + @"\" + DateTime.Now.Year.ToString("D4") + "_" + DateTime.Now.Month.ToString("D2") + "_" + DateTime.Now.Day.ToString("D2") + "_" + DateTime.Now.Hour.ToString("D2") + "_" + DateTime.Now.Minute.ToString("D2") + ".txt";

            using (StreamWriter sw = new StreamWriter(file))
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("----------------------------------------------------------------------------------------------------serialport----------------------------------------------------------------------------------------------------");
                sb.AppendLine(rtbLog.Text);
                sb.AppendLine("----------------------------------------------------------------------------------------------------USB----------------------------------------------------------------------------------------------------");
                sb.AppendLine(rtbUSB.Text);
                sb.AppendLine("----------------------------------------------------------------------------------------------------runtime----------------------------------------------------------------------------------------------------");
                sb.AppendLine(rtbSerialport.Text);

                sw.Write(sb);
            }
        }

    }
}
