using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FCT.ViewLayer.FormLibrary
{

    public partial class frmKeybutton : Form
    {
        public frmKeybutton()
        {
            InitializeComponent();
            TopMost = true;
        }

        public HashSet<string> targetButtons = new HashSet<string> { "0b", "08", "0a", "0f", "10" };
        public HashSet<string> clickedButtons = new HashSet<string>();

        private void frmKeybutton_Load(object sender, EventArgs e)
        {
            btnPower.Enabled = false;
        }

        public void OnKeyButtonTested(object sender, KeyButtonTestedEventArgs e)
        {
            foreach (string code in e.KeyCodes)
            {
                clickedButtons.Add(code);
                Invoke(new Action(() =>
                {
                    //0xc,0x8,0xa,0xf,0x10
                    if (code == "0b")
                    {
                        btnDownload.BackColor = Color.Lime;
                    }
                    else if (code == "0a")
                    {
                        btnOK.BackColor = Color.Lime;
                    } 
                    else if (code == "0f") btnNext.BackColor = Color.Lime;
                    else if (code == "10") btnLast.BackColor = Color.Lime;
                    else if (code == "08") btnMenu.BackColor = Color.Lime;
                }));
            }
        }

        public bool GetKeyButtonTestResult()
        {
            return targetButtons.SetEquals(clickedButtons);
        }

    }



}
