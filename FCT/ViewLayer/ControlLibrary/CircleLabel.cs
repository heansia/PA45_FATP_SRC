using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FCT.ViewLayer.ControlLibrary
{
    public class CircleLabel : Control
    {
        public CircleLabel()
        {
            // 1. 开启双缓冲，防止绘制时闪烁
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.SupportsTransparentBackColor, true);

            // 2. 默认设置
            this.DoubleBuffered = true;
            this.BackColor = Color.DodgerBlue; // 默认圆形背景色
            this.ForeColor = Color.White;      // 默认文字颜色
            this.Size = new Size(100, 100);    // 默认大小
            this.Font = new Font("Calibri", 9F, FontStyle.Regular);
            this.Text = "Circle";

            //this.OnPaint += OnPaint;
        }

        #region 自定义属性

        private Color _borderColor = Color.Transparent;
        /// <summary>
        /// 圆形边框颜色
        /// </summary>
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; this.Invalidate(); }
        }

        private int _borderWidth = 0;
        /// <summary>
        /// 圆形边框宽度
        /// </summary>
        public int BorderWidth
        {
            get => _borderWidth;
            set { _borderWidth = value; this.Invalidate(); }
        }

        #endregion


        // 【关键】当控件大小改变时，重新计算圆形区域
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }


        // 更新 Region 为圆形
        private void UpdateRegion()
        {
            // 获取客户区大小
            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;

            // 创建图形路径
            GraphicsPath path = new GraphicsPath();

            // 计算内边距（为了留出边框的空间，如果没有边框则不需要减）
            int padding = 0;
            if (this.Width > 0 && this.Height > 0)
            {
                // 添加一个椭圆，范围是整个控件区域
                // 注意：这里减去1是为了防止边缘渲染问题
                path.AddEllipse(0, 0, width - 1, height - 1);

                // 将路径应用为控件的区域
                this.Region = new Region(path);
            }

            path.Dispose();
        }

        #region 核心绘制逻辑

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // 开启抗锯齿

            // 绘制逻辑保持不变
            int diameter = Math.Min(this.Width, this.Height);
            int x = (this.Width - diameter) / 2;
            int y = (this.Height - diameter) / 2;
            Rectangle rect = new Rectangle(x, y, diameter - 1, diameter - 1);

            // ================= 核心修改：使用路径渐变画笔 =================
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(rect);

                // 创建路径渐变画笔 (PathGradientBrush)
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    // 1. 中心颜色：白色 (或者浅蓝白色 Color.FromArgb(200, 230, 255))
                    brush.CenterColor = Color.White;

                    // 2. 边缘颜色：你的基础蓝色 (DodgerBlue)
                    brush.SurroundColors = new Color[] { this.BackColor };

                    // 3. (可选) 调整光晕的中心点位置，0.5f 是正中心，可以改成 0.3f 让光晕偏左上角
                    brush.CenterPoint = new PointF(rect.X + rect.Width * 0.3f, rect.Y + rect.Height * 0.3f);

                    // 填充圆形
                    g.FillEllipse(brush, rect);
                }
            }

            // 画文字
            if (!string.IsNullOrEmpty(this.Text))
            {
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                TextRenderer.DrawText(g, this.Text, this.Font, rect, this.ForeColor, flags);
            }
        }
        #endregion

        #region 辅助功能

        /// <summary>
        /// 强制控件保持正方形，确保画出来的是正圆而不是椭圆
        /// </summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            // 当宽度改变时，强制高度等于宽度
            if (width != height)
            {
                height = width;
            }
            base.SetBoundsCore(x, y, width, height, specified);
        }

        /// <summary>
        /// 处理透明背景：让父控件重绘背景，实现真正的透明
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (this.BackColor == Color.Transparent && this.Parent != null)
            {
                // 获取父控件的绘制区域并让父控件绘制
                Rectangle rect = new Rectangle(this.Left, this.Top, this.Width, this.Height);
                // 触发父控件重绘该区域
                //ControlPaint.DrawTransparentBackground(e.Graphics, rect, this.Parent.BackColor);
                // 注意：如果父控件有背景图，这里需要更复杂的处理，普通纯色背景用上面这句即可
            }
            else
            {
                base.OnPaintBackground(e);
            }
        }

        #endregion


    }
}
