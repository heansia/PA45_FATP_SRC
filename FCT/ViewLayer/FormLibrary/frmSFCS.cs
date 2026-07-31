using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using FCT.Models;
using FCT.Models.SFC;

namespace FCT.ViewLayer.FormLibrary
{
    public partial class frmSFCS : Form
    {

        public event MESSettingChangedEventHanlder MESSettingChanged;

        public SFCS_Setting _sfcs { get; }
        public frmSFCS(SFCS_Setting sfcs)
        {
            InitializeComponent();

            _sfcs = sfcs;
            txtProduct.Text = sfcs.Product;
            txtPartNumber.Text = sfcs.PartNumber;
            txtStation.Text = sfcs.Station;
            txtBarcode.Text = sfcs.Barcode;
            txtLineNo.Text = sfcs.Line;
            txtVersion.Text = sfcs.testVersion;

            txtBarcode.Enabled = false;
            //txtLineNo.Enabled = false;
            txtVersion.Enabled = false;
            this.TopMost = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSetting();
            Hide();
        }
        public void SaveSetting()
        {
            _sfcs.Product = txtProduct.Text;
            _sfcs.PartNumber = txtPartNumber.Text;
            _sfcs.Station = txtStation.Text;
            _sfcs.Barcode = txtBarcode.Text;
            _sfcs.Line = txtLineNo.Text;
            _sfcs.testVersion = txtVersion.Text; 

            MESSettingChanged?.Invoke(this, new MESSettingChangedEventArgs(_sfcs));
        }

      
    }
}
