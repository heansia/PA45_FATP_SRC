using System.Windows.Forms;
using FCT.Models;

namespace FCT.ViewLayer.ControlLibrary
{
    public partial class uCtrl_TestItem : UserControl
    {
        public uCtrl_TestItem(ITestItem item)
        {
            InitializeComponent();
        }

        private uCtrl_TestItem()
        {
            InitializeComponent();
        }
    }
}
