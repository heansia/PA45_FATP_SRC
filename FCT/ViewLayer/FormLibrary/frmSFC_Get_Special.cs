using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FCT.Models.SFC.Interface;

namespace FCT.ViewLayer.FormLibrary
{
    public partial class frmSFC_Get_Special : Form
    {
        public frmSFC_Get_Special()
        {
            InitializeComponent();
        }

        private ISFC sfcInstance = new Models.SFC.SSG_SFC();
        private Thread _trigger;

        private void frmSFC_Get_Special_Load(object sender, EventArgs e)
        {

        }

        private void btnDo_Click(object sender, EventArgs e)
        {
            
        }

        private void StartTest()
        {
            if (_trigger != null && _trigger.IsAlive)
            {
                MessageBox.Show("_trigger thread is already running.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var start = new ThreadStart(delegate { go_through_testing_process(); });
            _trigger = new Thread(start);
            _trigger.Start();
        }

        private void go_through_testing_process()
        {
            List<string> source_datas = new List<string>();
            source_datas.RemoveAll(s => string.IsNullOrWhiteSpace(s));
            string product = "Scanner";
            string type = "";
            string para = "";

            if (rdbGetMainBarcode.Checked)
            {
                type = "GET_BARCODE";
            }

            if (rdbGetSubPartBarcode.Checked)
            {
                type = "GET_BARCODE_PART";

               
            }

            if (rdbGetModel.Checked)
            {
                type = "GET_SN_MODEL";
            }

            

            for (int i = 0; i < source_datas.Count; i++)
            {
                if (rdbGetSubPartBarcode.Checked)
                {
                    para = $"{source_datas[i]};{""}";
                }

                string value = sfcInstance.GetSpecialValue(product, type, para);

                this.rtbOutput.AppendText(value + "\n");
            }

        }
    }

    enum Get_Special_Value
    {
        Main_Barcode,
        Sub_Part_Barcode,
        Model
    }

}
