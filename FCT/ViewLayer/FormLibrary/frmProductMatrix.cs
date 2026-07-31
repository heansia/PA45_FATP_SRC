using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FCT.Models;
using FCT.Models.Products;
using FCT.Models.Products.Interface;

namespace FCT.ViewLayer.FormLibrary
{
    public partial class frmProductMatrix : Form
    {
        public IProductConfig ProductConfig { get; set; }

        public DataGridViewRow selectedRow;

        public frmProductMatrix(DataTable productMatrix)
        {
            InitializeComponent();
            dgvProductMatrix.DataSource = productMatrix;
        }

        private void frmProductMatrix_Load(object sender, EventArgs e)
        {
            dgvProductMatrix.Refresh();
        }

        private void dgvProductMatrix_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // 防止点到表头
            if (e.RowIndex < 0) return;

            // 获取当前双击的行
            selectedRow = dgvProductMatrix.Rows[e.RowIndex];

            // ==============================================
            // ✅ 获取这一行【所有单元格的值】
            // ==============================================
            //string sn = selectedRow.Cells["SerialNumber"].Value?.ToString() ?? "";
            //string model = selectedRow.Cells["ModelName"].Value?.ToString() ?? "";
            //string status = selectedRow.Cells["Status"].Value?.ToString() ?? "";

            // 你想拿多少列就拿多少列

            //MessageBox.Show($"SN：{sn}\n型号：{model}\n状态：{status}");
        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            if (selectedRow == null)
            {
                MessageBox.Show(
                            $"Please double-click the table to select the target test model.",
                            Global.AssemblyName,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                return;
            }

            if (ProductConfig == null)
            {
                ProductConfig = new ProductConfig();

            }

            Type type = ProductConfig.GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                // 只给 可以写入 的属性赋值
                if (selectedRow.Cells[prop.Name] != null)
                {
                    if (prop.CanWrite)
                    {
                        // 赋值！
                        prop.SetValue(ProductConfig, selectedRow.Cells[prop.Name].Value);
                    }
                }
            }

            DialogResult = DialogResult.Yes;
            Hide();
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            this.Hide();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Hide();
        }
    }
}
