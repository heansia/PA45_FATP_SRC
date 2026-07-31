using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using FCT.Models;

namespace FCT.ViewLayer.FormLibrary
{
    public partial class frmSerialPort : Form
    {
        private SerialPort _seri;

        /*
        private int _seriBoudRete = 0;
        public int SeriBoudRete
        {
            get { return _seriBoudRete; }
            set { _seriBoudRete = value; }
        }
        */


        public string SeriBoudRete
        {
            get;
            set;
        }

        public frmSerialPort()
        {
            InitializeComponent();
            init_baudretas_ui();
        }

        public frmSerialPort(SerialPort seri)
        {
            InitializeComponent();
            init_baudretas_ui();
            this._seri = seri;
        }

        private void init_baudretas_ui()
        {
            if (cmbBaudRates != null || (cmbBaudRates.Items.Count > 0)) cmbBaudRates.Items.Clear();

            foreach (int rate in SerialCommon.BoudRateList)
                cmbBaudRates.Items.Add(string.Format($"{rate}"));

            cmbBaudRates.SelectedIndex = 0;
        }

        private void init_comports_ui()
        {


        }


        private void frmSerialPort_Load(object sender, EventArgs e)
        {

        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            SeriBoudRete = cmbBaudRates.SelectedItem.ToString();
            this.DialogResult = DialogResult.Yes;
            this.Hide();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Hide();
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            this.Hide();
        }

      
    }
}
