using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MaterialSkin.Controls
{
    public class MaterialSlider : Control, IMaterialControl
    {
        #region "Private members"
        private bool _mousePressed;
        private int _mouseX;
        private bool _hovered = false;
        private Rectangle _indicatorRectangle;
        private Rectangle _indicatorRectangleNormal;
        private Rectangle _indicatorRectanglePressed;
        private Rectangle _textRectangle;
        private Rectangle _valueRectangle;
        private Rectangle _sliderRectangle;

        private const int _activeTrack = 6;
        private const int _inactiveTrack = 4;
        private const int _thumbRadius = 20;
        private const int _thumbRadiusHoverPressed = 40;
        #endregion

        #region "Public Properties"
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        private int _value;
        [DefaultValue(50)]
        [Category("Material Skin")]
        [Description("Define control value")]
        public int Value
        {
            get { return _value; }
            set
            {
                if (value < _rangeMin)
                    _value = _rangeMin;
                else if (value > _rangeMax)
                    _value = _rangeMax;
                else
                    _value = value;

                // Calculate the X position based on value
                _mouseX = CalculatePositionFromValue(_value);
                RecalculateIndicator();
            }
        }

        private int _valueMax;
        [DefaultValue(0)]
        [Category("Material Skin")]
        [Description("Define position indicator maximum value. Ignored when set to 0.")]
        public int ValueMax
        {
            get { return _valueMax; }
            set
            {
                if (value > _rangeMax)
                    _valueMax = _rangeMax;
                else if (value < _rangeMin)
                    _valueMax = _rangeMin;
                else
                    _valueMax = value;
            }
        }

        private int _rangeMax;
        [DefaultValue(100)]
        [Category("Material Skin")]
        [Description("Define control range maximum value")]
        public int RangeMax
        {
            get { return _rangeMax; }
            set
            {
                _rangeMax = value;
                _mouseX = CalculatePositionFromValue(_value);
                RecalculateIndicator();
            }
        }

        private int _rangeMin;
        [DefaultValue(0)]
        [Category("Material Skin")]
        [Description("Define control range minimum value")]
        public int RangeMin
        {
            get { return _rangeMin; }
            set
            {
                _rangeMin = value;
                _mouseX = CalculatePositionFromValue(_value);
                RecalculateIndicator();
            }
        }

        private string _text;
        [DefaultValue("MyData")]
        [Category("Material Skin")]
        [Description("Set control text")]
        public override string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                UpdateRects();
                Invalidate();
            }
        }

        private string _valueSuffix;
        [DefaultValue("")]
        [Category("Material Skin")]
        [Description("Set control value suffix text")]
        public string ValueSuffix
        {
            get { return _valueSuffix; }
            set
            {
                _valueSuffix = value;
                UpdateRects();
            }
        }

        private bool _showText;
        [DefaultValue(true)]
        [Category("Material Skin"), DisplayName("Show text")]
        [Description("Show text")]
        public bool ShowText
        {
            get { return _showText; }
            set { _showText = value; UpdateRects(); Invalidate(); }
        }

        private bool _showValue;
        [DefaultValue(true)]
        [Category("Material Skin"), DisplayName("Show value")]
        [Description("Show value")]
        public bool ShowValue
        {
            get { return _showValue; }
            set { _showValue = value; UpdateRects(); Invalidate(); }
        }

        private bool _useAccentColor;
        [Category("Material Skin"), DefaultValue(false), DisplayName("Use Accent Color")]
        public bool UseAccentColor
        {
            get { return _useAccentColor; }
            set { _useAccentColor = value; Invalidate(); }
        }

        private MaterialSkinManager.fontType _fontType = MaterialSkinManager.fontType.Body1;
        [Category("Material Skin"),
        DefaultValue(typeof(MaterialSkinManager.fontType), "Body1")]
        public MaterialSkinManager.fontType FontType
        {
            get { return _fontType; }
            set
            {
                _fontType = value;
                Font = SkinManager.getFontByType(_fontType);
                Refresh();
            }
        }
        #endregion

        #region "Events"
        [Category("Behavior")]
        [Description("Occurs when value change.")]
        public delegate void ValueChanged(object sender, int newValue);
        public event ValueChanged onValueChanged;
        #endregion

        public MaterialSlider()
        {
            SetStyle(ControlStyles.Selectable, true);
            ForeColor = SkinManager.TextHighEmphasisColor;
            RangeMax = 100;
            RangeMin = 0;
            Size = new Size(250, _thumbRadiusHoverPressed);
            Text = "My Data";
            Value = 50;
            ValueSuffix = "";
            ShowText = true;
            ShowValue = true;
            UseAccentColor = false;

            UpdateRects();
            DoubleBuffered = true;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Height = _thumbRadiusHoverPressed;
            UpdateRects();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && e.Y > _indicatorRectanglePressed.Top && e.Y < _indicatorRectanglePressed.Bottom)
            {
                _mousePressed = true;
                UpdateValue(e);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int newValue = _value + e.Delta / -40;

            if (_valueMax != 0 && newValue > _valueMax)
                Value = _valueMax;
            else
                Value = newValue;

            onValueChanged?.Invoke(this, _value);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            if (!this.Focused) this.Focus();
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            if (this.Focused) this.Parent.Focus();
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _mousePressed = false;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_mousePressed)
            {
                UpdateValue(e);
            }
        }

        private void UpdateValue(MouseEventArgs e)
        {
            int v = 0;

            // Calculate mouse position and constrain it to the slider bounds
            if (e.X >= _sliderRectangle.X + (_thumbRadius / 2) && e.X <= _sliderRectangle.Right - _thumbRadius / 2)
            {
                _mouseX = e.X - _thumbRadius / 2;
                double valuePerPx = ((double)(RangeMax - RangeMin)) / (_sliderRectangle.Width - _thumbRadius);
                v = (int)(valuePerPx * (_mouseX - _sliderRectangle.X)) + _rangeMin;
            }
            else if (e.X < _sliderRectangle.X)
            {
                _mouseX = _sliderRectangle.X;
                v = _rangeMin;
            }
            else if (e.X > _sliderRectangle.Right - _thumbRadius)
            {
                _mouseX = _sliderRectangle.Right - _thumbRadius;
                v = _rangeMax;
            }

            // Apply value max constraint if specified
            if (_valueMax != 0 && v > _valueMax)
            {
                Value = _valueMax;
            }
            else
            {
                if (v != _value)
                {
                    _value = v;
                    onValueChanged?.Invoke(this, _value);
                }
                RecalculateIndicator();
            }
        }

        private void UpdateRects()
        {
            Size textSize;
            Size valueSize;

            using (NativeTextRenderer nativeText = new NativeTextRenderer(CreateGraphics()))
            {
                textSize = nativeText.MeasureLogString(_showText ? Text : "", SkinManager.getLogFontByType(_fontType));
                valueSize = nativeText.MeasureLogString(_showValue ? RangeMax.ToString() + _valueSuffix : "", SkinManager.getLogFontByType(_fontType));
            }

            _valueRectangle = new Rectangle(Width - valueSize.Width - _thumbRadiusHoverPressed / 4, 0, valueSize.Width + _thumbRadiusHoverPressed / 4, Height);
            _textRectangle = new Rectangle(0, 0, textSize.Width + _thumbRadiusHoverPressed / 4, Height);
            _sliderRectangle = new Rectangle(_textRectangle.Right, 0, _valueRectangle.Left - _textRectangle.Right, _thumbRadius);

            _mouseX = CalculatePositionFromValue(_value);
            RecalculateIndicator();
        }

        // Helper method to calculate X position from a value
        private int CalculatePositionFromValue(int value)
        {
            // Ensure we have a valid range
            if (_rangeMax <= _rangeMin)
                return _sliderRectangle.X;

            double valuePercent = (double)(value - _rangeMin) / (_rangeMax - _rangeMin);
            return _sliderRectangle.X + (int)(valuePercent * (_sliderRectangle.Width - _thumbRadius));
        }

        private void RecalculateIndicator()
        {
            _indicatorRectangle = new Rectangle(_mouseX, (Height - _thumbRadius) / 2, _thumbRadius, _thumbRadius);
            _indicatorRectangleNormal = new Rectangle(_indicatorRectangle.X, Height / 2 - _thumbRadius / 2, _thumbRadius, _thumbRadius);
            _indicatorRectanglePressed = new Rectangle(_indicatorRectangle.X + _thumbRadius / 2 - _thumbRadiusHoverPressed / 2, Height / 2 - _thumbRadiusHoverPressed / 2, _thumbRadiusHoverPressed, _thumbRadiusHoverPressed);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.Clear(Parent.BackColor);

            Color _inactiveTrackColor;
            Color _accentColor;
            Brush _accentBrush;
            Brush _disabledBrush;
            Color _disabledColor;
            Color _thumbHoverColor;
            Color _thumbPressedColor;

            // Determine accent color based on settings
            _accentColor = _useAccentColor
                ? SkinManager.ColorScheme.AccentColor
                : SkinManager.ColorScheme.PrimaryColor;

            _accentBrush = new SolidBrush(_accentColor);
            _disabledBrush = new SolidBrush(Color.FromArgb(255, 158, 158, 158));

            // Adjust colors based on theme
            if (SkinManager.Theme == MaterialSkinManager.Themes.DARK)
            {
                _disabledColor = Color.FromArgb((int)(2.55 * 30), 255, 255, 255);
                _inactiveTrackColor = _accentColor.Darken(0.25f);
            }
            else
            {
                _disabledColor = Color.FromArgb((int)(2.55 * (_hovered ? 38 : 26)), 0, 0, 0);
                _inactiveTrackColor = _accentColor.Lighten(0.6f);
            }

            _thumbHoverColor = Color.FromArgb((int)(2.55 * 15), _accentColor);
            _thumbPressedColor = Color.FromArgb((int)(2.55 * 30), _accentColor);

            // Create track paths
            GraphicsPath _inactiveTrackPath = DrawHelper.CreateRoundRect(
                _sliderRectangle.X + (_thumbRadius / 2),
                _sliderRectangle.Y + Height / 2 - _inactiveTrack / 2,
                _sliderRectangle.Width - _thumbRadius,
                _inactiveTrack,
                2);

            GraphicsPath _activeTrackPath = DrawHelper.CreateRoundRect(
                _sliderRectangle.X + (_thumbRadius / 2),
                _sliderRectangle.Y + Height / 2 - _activeTrack / 2,
                _indicatorRectangleNormal.X - _sliderRectangle.X,
                _activeTrack,
                2);

            // Draw enabled or disabled slider
            if (Enabled)
            {
                // Draw inactive track
                g.FillPath(new SolidBrush(_inactiveTrackColor), _inactiveTrackPath);

                // Draw active track
                g.FillPath(_accentBrush, _activeTrackPath);

                // Draw thumb
                if (_mousePressed)
                {
                    g.FillEllipse(_accentBrush, _indicatorRectangleNormal);
                    g.FillEllipse(new SolidBrush(_thumbPressedColor), _indicatorRectanglePressed);
                }
                else
                {
                    g.FillEllipse(_accentBrush, _indicatorRectangleNormal);

                    if (_hovered)
                    {
                        g.FillEllipse(new SolidBrush(_thumbHoverColor), _indicatorRectanglePressed);
                    }
                }
            }
            else
            {
                // Draw inactive track (disabled)
                g.FillPath(new SolidBrush(_disabledColor.Lighten(0.25f)), _inactiveTrackPath);

                // Draw active track (disabled)
                g.FillPath(_disabledBrush, _activeTrackPath);
                g.FillEllipse(_disabledBrush, _indicatorRectangleNormal);
            }

            // Draw text and value
            using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
            {
                if (_showText)
                {
                    // Draw text
                    nativeText.DrawTransparentText(
                        Text,
                        SkinManager.getLogFontByType(_fontType),
                        Enabled ? SkinManager.TextHighEmphasisColor : SkinManager.TextDisabledOrHintColor,
                        _textRectangle.Location,
                        _textRectangle.Size,
                        NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                }

                if (_showValue)
                {
                    // Draw value
                    nativeText.DrawTransparentText(
                        Value.ToString() + ValueSuffix,
                        SkinManager.getLogFontByType(_fontType),
                        Enabled ? SkinManager.TextHighEmphasisColor : SkinManager.TextDisabledOrHintColor,
                        _valueRectangle.Location,
                        _valueRectangle.Size,
                        NativeTextRenderer.TextAlignFlags.Right | NativeTextRenderer.TextAlignFlags.Middle);
                }
            }

            // Dispose brushes
            _accentBrush.Dispose();
            _disabledBrush.Dispose();
        }
    }
}