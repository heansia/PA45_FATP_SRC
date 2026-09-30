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
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using FCT.Models;
using INI = FCT.Util.INI_Parser;

namespace FCT.ViewLayer.FormLibrary
{
    public partial class frmTestItems : Form
    {
        public string TestItemFile { get; set; }
        public frmTestItems(string file)
        {
            InitializeComponent();
            TestItemFile = file;
            txtIndex.Enabled = false;
            reformat_ini_file();
        }

        private void btnReformat_Click(object sender, EventArgs e)
        {
            reformat_ini_file();
            MessageBox.Show("Reformat TEST ITEMS OK.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void reformat_ini_file()
        {
            StringBuilder sb = new StringBuilder();

            int index = 1;
            using (StreamReader sr = new StreamReader(TestItemFile))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("["))
                    {
                        int index_start = line.IndexOf("[");
                        int index_end = line.IndexOf("]");

                        string temp_index = line.Substring(index_start + 1, index_end - index_start - 1);

                        if (int.TryParse(temp_index, out int intValue))
                        {
                            line = "[" + index + "]";
                            index = index + 1;
                        }
                    }

                    sb.AppendLine(line);
                }
            }

            using (StreamWriter sw = new StreamWriter(TestItemFile)) { sw.Write(sb); }

            INI.SetValue("TestCount", "Count", (index - 1).ToString(), TestItemFile);
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            InitializeTestItem(listView1);
        }

        private void InitializeTestItem(System.Windows.Forms.ListView listview)
        {
            listview.Items.Clear();
            listview.Columns.Clear();
            listview.View = View.Details;

            listview.Columns.Add("Index", 50, HorizontalAlignment.Left);
            listview.Columns.Add("Enable", 50, HorizontalAlignment.Center);
            listview.Columns.Add("Name", 100, HorizontalAlignment.Center);
            listview.Columns.Add("Upper Limit", 100, HorizontalAlignment.Center);
            listview.Columns.Add("Lower Limit", 100, HorizontalAlignment.Center);
            listview.Columns.Add("Description", 100, HorizontalAlignment.Center);
            //listview.Columns.Add("Value", 100, HorizontalAlignment.Center);
            //listview.Columns.Add("Result", 100, HorizontalAlignment.Center);

            int count = Convert.ToInt32(INI.GetValue("TestCount", "Count", TestItemFile));

            for (int i = 1; i < count + 1; i++)
            {
                string enable = Convert.ToBoolean(INI.GetValue(i.ToString(), "TestEnable", TestItemFile)).ToString();
                string name = INI.GetValue(i.ToString(), "TestName", TestItemFile);
                string upperLimit = INI.GetValue(i.ToString(), "TestUpperLimit", TestItemFile);
                string lowerLimit = INI.GetValue(i.ToString(), "TestLowerLimit", TestItemFile);
                string description = INI.GetValue(i.ToString(), "Description", TestItemFile);

                ListViewItem lvItem = new ListViewItem(i.ToString());
                lvItem.SubItems.Add(enable);
                lvItem.SubItems.Add(name);
                lvItem.SubItems.Add(upperLimit);
                lvItem.SubItems.Add(lowerLimit);
                lvItem.SubItems.Add(description);

                listview.Items.Add(lvItem);
            }
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hitTest = listView1.HitTest(e.X, e.Y);
            ListViewItem clickedItem = hitTest.Item;

            if (clickedItem != null) OutputListViewItemValues(clickedItem);
        }

        private void OutputListViewItemValues(ListViewItem item)
        {
            txtIndex.Text = item.SubItems[0].Text;
            //txtEnable.Text = item.SubItems[1].Text;
            ckbEnable.Checked = Convert.ToBoolean(item.SubItems[1].Text);
            txtName.Text = item.SubItems[2].Text;
            txtUpperLimit.Text = item.SubItems[3].Text;
            txtLowerLimit.Text = item.SubItems[4].Text;
            txtDescription.Text = item.SubItems[5].Text;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtIndex.Text))
            {
                MessageBox.Show("the key is empty!!!", "Nomad ATE", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            INI.SetValue(txtIndex.Text, "TestEnable", ckbEnable.Checked.ToString(), TestItemFile);
            INI.SetValue(txtIndex.Text, "TestName", txtName.Text, TestItemFile);
            INI.SetValue(txtIndex.Text, "TestUpperLimit", txtUpperLimit.Text, TestItemFile);
            INI.SetValue(txtIndex.Text, "TestLowerLimit", txtLowerLimit.Text, TestItemFile);
            INI.SetValue(txtIndex.Text, "Description", txtDescription.Text, TestItemFile);

            MessageBox.Show(
                          "Change Item spec OK.",
                          Global.AssemblyName,
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Information);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string enable = true.ToString();
            string name = "";
            string upperLimit = "";
            string lowerLimit = "";
            string description = "";

            ListViewItem lvItem = new ListViewItem((listView1.Items.Count + 1).ToString());
            lvItem.SubItems.Add(enable);
            lvItem.SubItems.Add(name);
            lvItem.SubItems.Add(upperLimit);
            lvItem.SubItems.Add(lowerLimit);
            lvItem.SubItems.Add(description);

            listView1.Items.Add(lvItem);

            /*
            using (StreamWriter sw = new StreamWriter(TestItemFile, true))
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("[" + txtIndex.Text + "]");
                sb.AppendLine("TestEnable = " + txtEnable.Text);
                sb.AppendLine("TestName = " + txtName.Text);
                sb.AppendLine("TestUpperLimit = " + txtUpperLimit.Text);
                sb.AppendLine("TestLowerLimit = " + txtLowerLimit.Text);
                sb.AppendLine("Description = " + txtDescription.Text);
                sw.WriteLine(sb);
            }
            */

        }

        private void frmTestItems_Load(object sender, EventArgs e)
        {

        }
    }
}



