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
    public class RoundLabel : Label
    { // 圆角半径，越大越圆
        private int _cornerRadius = 10;
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        // 边框颜色
        private Color _borderColor = Color.Transparent;
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        // 边框宽度
        private int _borderWidth = 1;
        public int BorderWidth
        {
            get => _borderWidth;
            set { _borderWidth = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent.BackColor);

            // 画背景圆角矩形
            using (var brush = new SolidBrush(BackColor))
            {
                var path = GetRoundedRect(ClientRectangle, CornerRadius);
                g.FillPath(brush, path);
            }

            // 画边框
            if (BorderColor != Color.Transparent && BorderWidth > 0)
            {
                using (var pen = new Pen(BorderColor, BorderWidth))
                {
                    var path = GetRoundedRect(ClientRectangle, CornerRadius);
                    g.DrawPath(pen, path);
                }
            }

            // 画文字（保持原Label样式）
            using (var brush = new SolidBrush(ForeColor))
            {
                var format = new StringFormat();
                // 对齐方式和原Label保持一致
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;

                g.DrawString(Text, Font, brush, ClientRectangle, format);
            }
        }

        // 生成圆角矩形路径
        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float x = rect.X;
            float y = rect.Y;
            float w = rect.Width;
            float h = rect.Height;

            path.AddArc(x, y, radius, radius, 180, 90);
            path.AddArc(x + w - radius, y, radius, radius, 270, 90);
            path.AddArc(x + w - radius, y + h - radius, radius, radius, 0, 90);
            path.AddArc(x, y + h - radius, radius, radius, 90, 90);
            path.CloseAllFigures();

            return path;
        }
    }
}
