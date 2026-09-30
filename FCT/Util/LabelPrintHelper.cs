using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FCT.Util
{
    public static class LabelPrintHelper
    {
        public static bool PrintLabel(string label_file, string[] keys, string[] values)
        {
            bool rlt = false;
            LabelManager2.Application LabApp = new LabelManager2.Application();
            LabelManager2.IDocument doc;
            LabApp.Visible = false;
            LabApp.Documents.Open(label_file, false);
            doc = LabApp.ActiveDocument;

            try
            {
                for (int i = 0; i < keys.Length; i++) { doc.Variables.Item(keys[i]).Value = values[i]; }

                //doc.Variables.Item(key).Value = value;
                doc.PrintDocument(1);
                doc.FormFeed();
                rlt = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, string.Format($"FUNC [ {MethodBase.GetCurrentMethod().Name} ]"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                rlt = false;
            }

            LabApp.Quit();
            return rlt;
        }

        public static bool PrintLabel(string label_file, string key, string value)
        {
            bool rlt = false;
            LabelManager2.Application LabApp = new LabelManager2.Application();
            LabelManager2.IDocument doc;
            LabApp.Visible = false;
            LabApp.Documents.Open(label_file, false);
            doc = LabApp.ActiveDocument;

            try
            {
                doc.Variables.Item(key).Value = value;
                doc.PrintDocument(1);
                doc.FormFeed();
                rlt = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, string.Format($"FUNC [ {MethodBase.GetCurrentMethod().Name} ]"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                rlt = false;
            }

            LabApp.Quit();
            return rlt;
        }
    }
}
