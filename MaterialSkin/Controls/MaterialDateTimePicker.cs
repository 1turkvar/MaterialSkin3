namespace MaterialSkin.Controls
{
    using MaterialSkin;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public class MaterialDateTimePicker : DateTimePicker, IMaterialControl
    {
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        [Category("Material Skin")]
        public bool UseAccentColor { get; set; } = false;

        public MaterialDateTimePicker()
        {
            Format = DateTimePickerFormat.Short;
            Height = 36;
            BackColor = SkinManager.BackgroundColor;
            ForeColor = SkinManager.TextHighEmphasisColor;
            Font = SkinManager.getFontByType(MaterialSkinManager.fontType.Subtitle1);

            SetStyle(ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            g.Clear(Parent.BackColor);
            using (SolidBrush backBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(backBrush, ClientRectangle);
            }

            string displayText = Text;
            Color textColor = Enabled
                ? (UseAccentColor ? SkinManager.ColorScheme.AccentColor : ForeColor)
                : SkinManager.TextDisabledOrHintColor;

            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(displayText, Font, textBrush, new PointF(8, (Height - Font.Height) / 2));
            }

            // Drop-down arrow rectangle
            int arrowSize = Height - 16;
            Rectangle arrowRect = new Rectangle(Width - arrowSize - 8, 8, arrowSize, arrowSize);

            // Draw drop-down arrow
            using (SolidBrush arrowBrush = new SolidBrush(textColor))
            {
                Point[] triangle = new Point[]
                {
            new Point(arrowRect.Left + arrowRect.Width / 2 - 4, arrowRect.Top + arrowRect.Height / 2 - 2),
            new Point(arrowRect.Left + arrowRect.Width / 2 + 4, arrowRect.Top + arrowRect.Height / 2 - 2),
            new Point(arrowRect.Left + arrowRect.Width / 2, arrowRect.Top + arrowRect.Height / 2 + 3),
                };
                g.FillPolygon(arrowBrush, triangle);
            }

            // Border
            using (Pen borderPen = new Pen(SkinManager.DividersColor))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }


    }
}
