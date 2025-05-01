namespace MaterialSkin.Controls
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public class MaterialLabel : Label, IMaterialControl
    {
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        private ContentAlignment _textAlign = ContentAlignment.TopLeft;
        private NativeTextRenderer.TextAlignFlags _alignment;

        [DefaultValue(typeof(ContentAlignment), "TopLeft")]
        public override ContentAlignment TextAlign
        {
            get
            {
                return _textAlign;
            }
            set
            {
                _textAlign = value;
                UpdateAlignment();
                Invalidate();
            }
        }

        [Category("Material Skin"),
        DefaultValue(false)]
        public bool HighEmphasis { get; set; }

        [Category("Material Skin"),
        DefaultValue(false)]
        public bool UseAccent { get; set; }

        private MaterialSkinManager.fontType _fontType = MaterialSkinManager.fontType.Body1;

        [Category("Material Skin"),
        DefaultValue(typeof(MaterialSkinManager.fontType), "Body1")]
        public MaterialSkinManager.fontType FontType
        {
            get
            {
                return _fontType;
            }
            set
            {
                _fontType = value;
                Font = SkinManager.getFontByType(_fontType);
                Refresh();
            }
        }

        public MaterialLabel()
        {
            FontType = MaterialSkinManager.fontType.Body1;
            TextAlign = ContentAlignment.TopLeft;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            if (!AutoSize)
            {
                return proposedSize;
            }

            using (NativeTextRenderer nativeText = new NativeTextRenderer(CreateGraphics()))
            {
                Size strSize = nativeText.MeasureLogString(Text, SkinManager.getLogFontByType(_fontType));
                // Gerekli düzeltme için +1 piksel ekleme
                strSize.Width += 1;
                return strSize;
            }
        }

        private void UpdateAlignment()
        {
            switch (_textAlign)
            {
                case ContentAlignment.TopLeft:
                    _alignment = NativeTextRenderer.TextAlignFlags.Top | NativeTextRenderer.TextAlignFlags.Left;
                    break;

                case ContentAlignment.TopCenter:
                    _alignment = NativeTextRenderer.TextAlignFlags.Top | NativeTextRenderer.TextAlignFlags.Center;
                    break;

                case ContentAlignment.TopRight:
                    _alignment = NativeTextRenderer.TextAlignFlags.Top | NativeTextRenderer.TextAlignFlags.Right;
                    break;

                case ContentAlignment.MiddleLeft:
                    _alignment = NativeTextRenderer.TextAlignFlags.Middle | NativeTextRenderer.TextAlignFlags.Left;
                    break;

                case ContentAlignment.MiddleCenter:
                    _alignment = NativeTextRenderer.TextAlignFlags.Middle | NativeTextRenderer.TextAlignFlags.Center;
                    break;

                case ContentAlignment.MiddleRight:
                    _alignment = NativeTextRenderer.TextAlignFlags.Middle | NativeTextRenderer.TextAlignFlags.Right;
                    break;

                case ContentAlignment.BottomLeft:
                    _alignment = NativeTextRenderer.TextAlignFlags.Bottom | NativeTextRenderer.TextAlignFlags.Left;
                    break;

                case ContentAlignment.BottomCenter:
                    _alignment = NativeTextRenderer.TextAlignFlags.Bottom | NativeTextRenderer.TextAlignFlags.Center;
                    break;

                case ContentAlignment.BottomRight:
                    _alignment = NativeTextRenderer.TextAlignFlags.Bottom | NativeTextRenderer.TextAlignFlags.Right;
                    break;

                default:
                    _alignment = NativeTextRenderer.TextAlignFlags.Top | NativeTextRenderer.TextAlignFlags.Left;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent.BackColor);

            Color textColor = GetTextColor();

            // Draw Text
            using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
            {
                nativeText.DrawMultilineTransparentText(
                    Text,
                    SkinManager.getLogFontByType(_fontType),
                    textColor,
                    ClientRectangle.Location,
                    ClientRectangle.Size,
                    _alignment);
            }
        }

        private Color GetTextColor()
        {
            if (!Enabled)
            {
                return SkinManager.TextDisabledOrHintColor;
            }

            if (HighEmphasis)
            {
                if (UseAccent)
                {
                    return SkinManager.ColorScheme.AccentColor;
                }

                return (SkinManager.Theme == MaterialSkinManager.Themes.LIGHT) ?
                    SkinManager.ColorScheme.PrimaryColor :
                    SkinManager.ColorScheme.PrimaryColor.Lighten(0.25f);
            }

            return SkinManager.TextHighEmphasisColor;
        }

        protected override void InitLayout()
        {
            base.InitLayout();
            // Font constructor'da da ayarlandığı için burada tekrar ayarlamaya gerek yok
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}