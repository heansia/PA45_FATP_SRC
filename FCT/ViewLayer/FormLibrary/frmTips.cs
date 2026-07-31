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

namespace FCT.ViewLayer.FormLibrary
{
    public partial class frmTips : Form
    {
        public frmTips(string zh_cn_msg, string en_us_msg, string thai_msg)
        {
            InitializeComponent();

            lblTips_zh_cn.Text = zh_cn_msg;
            lblTips_en_us.Text = en_us_msg;
            lblTips_thai.Text = thai_msg;

            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent; // 相对于父窗口居中
            //this.Size = new System.Drawing.Size(626, 626); // 固定大小
        }

        public frmTips(string picture_path, string zh_cn_msg, string en_us_msg, string thai_msg)
        {
            InitializeComponent();

            if (File.Exists(picture_path))
            {
                picTips.Image = Image.FromFile(picture_path);
            }

            lblTips_zh_cn.Text = zh_cn_msg;
            lblTips_en_us.Text = en_us_msg;
            lblTips_thai.Text = thai_msg;

            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent; // 相对于父窗口居中
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes; // 设置对话框返回结果
            Close(); // 关闭对话框
            Dispose();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel; // 设置对话框返回结果
            Close(); // 关闭对话框
            Dispose();
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No; // 设置对话框返回结果
            Close(); // 关闭对话框
            Dispose();
        }
    }
}
