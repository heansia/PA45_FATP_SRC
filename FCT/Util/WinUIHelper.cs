using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;

namespace FCT.Util
{
    public static class WinUIHelper
    {// 枚举窗口回调
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        /// <summary>
        /// 根据进程ID 获取【顶级可见主窗口】句柄
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>窗口句柄，找不到返回 IntPtr.Zero</returns>
        public static IntPtr GetMainWindowHandleByProcessId(int processId)
        {
            IntPtr hwndResult = IntPtr.Zero;
            uint targetPid = (uint)processId;

            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);

                // 条件：PID匹配 + 顶级窗口（无父窗口）+ 窗口可见
                //if (pid == targetPid && GetParent(hWnd) == IntPtr.Zero && IsWindowVisible(hWnd))
                if (pid == targetPid)
                {
                    hwndResult = hWnd;
                    //return false; // 找到，停止枚举
                    //break;
                }
                return true;
            }, IntPtr.Zero);

            return hwndResult;
        }

        /// <summary>
        /// 获取该进程下所有顶级窗口（一个进程可能多个窗口）
        /// </summary>
        public static List<IntPtr> GetAllTopWindowHandlesByProcessId(int processId)
        {
            List<IntPtr> list = new List<IntPtr>();
            uint targetPid = (uint)processId;

            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == targetPid && GetParent(hWnd) == IntPtr.Zero)
                {
                    list.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);

            return list;
        }

        /// <summary>
        /// 深度遍历整棵UIA树，规避FindFirst无法穿透空名称容器的BUG
        /// </summary>
        private static AutomationElement DeepFindElement(AutomationElement root, Condition targetCond)
        {
            TreeWalker walker = TreeWalker.RawViewWalker;
            AutomationElement node = walker.GetFirstChild(root);

            while (node != null)
            {
                try
                {
                    // 判断当前节点是否满足条件：查找自身
                    AutomationElement selfMatch = node.FindFirst(TreeScope.Descendants, targetCond);
                    if (selfMatch != null)
                    {
                        return selfMatch;
                    }
                }
                catch
                {
                    // 部分无效元素读取异常，直接跳过
                }

                // 递归查找子节点
                AutomationElement childResult = DeepFindElement(node, targetCond);
                if (childResult != null)
                    return childResult;

                // 移动到下一个兄弟节点
                node = walker.GetNextSibling(node);
            }
            return null;
        }

        /// <summary>
        /// 增加超时重试，防止界面还未渲染完成
        /// </summary>
        public static AutomationElement DeepFindWithRetry(AutomationElement root, Condition cond, int timeoutMs = 5000)
        {
            int tick = 0;
            while (tick < timeoutMs)
            {
                var ele = DeepFindElement(root, cond);
                if (ele != null)
                    return ele;
                System.Threading.Thread.Sleep(150);
                tick += 150;
            }
            return null;
        }


        [DllImport("user32.dll")]

        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        //[DllImport("user32.dll")]
        //private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        // SetWindowPos 常量
        private static readonly IntPtr HWND_TOP = new IntPtr(0);        //放到Z序最上面（临时，别的窗口可以覆盖）
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);   //永久全局置顶
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        /// <summary>
        /// 解决Windows后台程序无法SetForegroundWindow限制，把窗口带到前台最上层
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        public static void ForceBringWindowToFront(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            uint targetThreadId = GetWindowThreadProcessId(hWnd, out _);
            uint currentThreadId = GetCurrentThreadId();

            // 线程附加，绕过Windows前台锁，用完一定要断开！
            if (targetThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, targetThreadId, true);
            }

            // 1. Z序放到最上层（临时，不永久置顶）
            SetWindowPos(hWnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            // 2. 激活窗口，拿到键盘焦点
            SetForegroundWindow(hWnd);

            // 必须断开Attach！否则会线程卡死风险
            if (targetThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }

        /// <summary>
        /// 截取主显示器完整屏幕
        /// </summary>
        /// <returns>屏幕位图</returns>
        public static Bitmap CapturePrimaryScreen()
        {
            Rectangle screenRect = Screen.PrimaryScreen.Bounds;
            Bitmap bmp = new Bitmap(screenRect.Width, screenRect.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // CopyFromScreen：屏幕DC拷贝到位图
                g.CopyFromScreen(
                    sourceX: screenRect.X,
                    sourceY: screenRect.Y,
                    destinationX: 0,
                    destinationY: 0,
                    blockRegionSize: screenRect.Size);
            }
            return bmp;
        }

    }
}
