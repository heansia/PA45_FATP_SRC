using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using PRINTER = FCT.Util.LabelPrintHelper;

namespace FCT.ViewLayer.FormLibrary
{
    public partial class frmACCPrint : Form
    {
        public frmACCPrint()
        {
            InitializeComponent();
            keys[0] = model_var_name;
            keys[1] = model_descp_var_name;
            keys[2] = pn_var_name;
            keys[3] = sn_var_name;
            keys[4] = mfd_var_name;

        }

        string lab_file_path = Path.Combine(Global.ASSEMBLY_PATH, "LabFile", "PA45_ACC.Lab");

        string model_var_name = "MODEL";
        string model_descp_var_name = "MODEL_DESCP";
        string pn_var_name = "PN";
        string sn_var_name = "SN";
        string mfd_var_name = "MFD";


        string[] vars = new string[5];
        string[] keys = new string[5];

        private void btnPrint1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSN1.Text) || string.IsNullOrWhiteSpace(txtSN1.Text))
            {
                MessageBox.Show("不能为空。");
                return;
            }

            vars[0] = lblModel1.Text;
            vars[1] = lblModelDescp1.Text;
            vars[2] = lblHonPN1.Text;
            vars[3] = txtSN1.Text;
            vars[4] = DateTime.Now.Year.ToString("D4") + DateTime.Now.Month.ToString("D2") + DateTime.Now.Day.ToString("D2");

            PRINTER.PrintLabel(lab_file_path, keys, vars);
            txtSN1.SelectAll();
        }

        private void btnPrint2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSN2.Text) || string.IsNullOrWhiteSpace(txtSN2.Text))
            {
                MessageBox.Show("不能为空。");
                return;
            }
            
            vars[0] = lblModel2.Text;
            vars[1] = lblModelDescp2.Text;
            vars[2] = lblHonPN2.Text;
            vars[3] = txtSN2.Text;
            vars[4] = DateTime.Now.Year.ToString("D4") + DateTime.Now.Month.ToString("D2") + DateTime.Now.Day.ToString("D2");

            PRINTER.PrintLabel(lab_file_path, keys, vars);
            txtSN2.SelectAll();
        }

        private void btnPrint3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSN3.Text) || string.IsNullOrWhiteSpace(txtSN3.Text))
            {
                MessageBox.Show("不能为空。");
                return;
            }
            
            vars[0] = lblModel3.Text;
            vars[1] = lblModelDescp3.Text;
            vars[2] = lblHonPN3.Text;
            vars[3] = txtSN3.Text;
            vars[4] = DateTime.Now.Year.ToString("D4") + DateTime.Now.Month.ToString("D2") + DateTime.Now.Day.ToString("D2");

            PRINTER.PrintLabel(lab_file_path, keys, vars);
            txtSN3.SelectAll();
        }
    }
}
