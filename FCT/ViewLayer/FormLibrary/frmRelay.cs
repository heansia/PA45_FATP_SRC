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
    public partial class frmRelay : Form
    {
       
        // 1. 私有静态实例（volatile 保证多线程下可见性）
        private static volatile frmRelay _instance;
        // 2. 锁对象（保证线程安全）
        private static readonly object _lockObj = new object();

        // 3. 私有构造函数
        private frmRelay()
        {
            InitializeComponent();
            this.TopMost = true;
            // 移除 FormClosing 拦截（允许用户真正关闭窗体）
        }

        // 4. 公开的静态属性：获取/创建唯一实例
        public static frmRelay Instance
        {
            get
            {
                // 双重检查锁：高性能 + 线程安全
                if (_instance == null || _instance.IsDisposed)
                {
                    lock (_lockObj)
                    {
                        if (_instance == null || _instance.IsDisposed)
                        {
                            _instance = new frmRelay();
                        }
                    }
                }
                return _instance;
            }
        }

        // 便捷显示方法
        public static void ShowInstance()
        {
            if (Instance.WindowState == FormWindowState.Minimized)
            {
                Instance.WindowState = FormWindowState.Normal;
            }
            Instance.Show();
            Instance.Activate();
           
        }

      
    }
}
