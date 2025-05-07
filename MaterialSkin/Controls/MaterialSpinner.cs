namespace MaterialSkin.Controls
{
    using MaterialSkin;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    /// <summary>
    /// Material Design stilinde Bootstrap benzeri spinner kontrolü
    /// </summary>
    public class MaterialSpinner : Control, IMaterialControl
    {
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        // Spinner tipleri
        public enum SpinnerStyle
        {
            Border,      // Bootstrap'teki border spinner
            Grow,        // Bootstrap'teki grow spinner
            Circle,      // Material Design'daki dairesel spinner
            Dots         // Nokta spinner
        }

        // Spinner boyutları
        public enum SpinnerSizeType
        {
            Small,
            Medium,
            Large
        }

        private SpinnerStyle _spinnerType = SpinnerStyle.Border;
        private SpinnerSizeType _spinnerSize = SpinnerSizeType.Medium;
        private Color _spinnerColor;
        private bool _useAccentColor = false;
        private float _animationSpeed = 1.0f;
        private float _animationValue = 0f;
        private Timer _animationTimer;
        private bool _isAnimating = true;

        [Category("Material Skin")]
        public SpinnerStyle Type
        {
            get => _spinnerType;
            set
            {
                _spinnerType = value;
                Invalidate();
            }
        }

        [Category("Material Skin")]
        public SpinnerSizeType SpinnerSize
        {
            get => _spinnerSize;
            set
            {
                _spinnerSize = value;
                UpdateControlSize();
                Invalidate();
            }
        }

        [Category("Material Skin")]
        public bool UseAccentColor
        {
            get => _useAccentColor;
            set
            {
                _useAccentColor = value;
                UpdateSpinnerColor();
                Invalidate();
            }
        }

        [Category("Material Skin")]
        public Color SpinnerColor
        {
            get => _spinnerColor;
            set
            {
                _spinnerColor = value;
                Invalidate();
            }
        }

        [Category("Material Skin")]
        public float AnimationSpeed
        {
            get => _animationSpeed;
            set
            {
                _animationSpeed = Math.Max(0.1f, Math.Min(3.0f, value));
                _animationTimer.Interval = (int)(40 / _animationSpeed);
            }
        }

        [Category("Material Skin")]
        public bool IsAnimating
        {
            get => _isAnimating;
            set
            {
                if (_isAnimating != value)
                {
                    _isAnimating = value;
                    if (_isAnimating)
                        _animationTimer.Start();
                    else
                        _animationTimer.Stop();
                }
            }
        }

        public MaterialSpinner()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;

            // Animasyon timer'ını başlat
            _animationTimer = new Timer();
            _animationTimer.Interval = 40;
            _animationTimer.Tick += (sender, e) =>
            {
                _animationValue += 0.05f * _animationSpeed;
                if (_animationValue >= 1.0f)
                    _animationValue = 0f;

                Invalidate();
            };

            UpdateControlSize();
            UpdateSpinnerColor();
            _animationTimer.Start();
        }

        private void UpdateControlSize()
        {
            int size = GetPixelSize();
            this.Size = new System.Drawing.Size(size, size);
            MinimumSize = new System.Drawing.Size(size, size);
        }

        private int GetPixelSize()
        {
            switch (_spinnerSize)
            {
                case SpinnerSizeType.Small:
                    return 16;
                case SpinnerSizeType.Large:
                    return 48;
                case SpinnerSizeType.Medium:
                default:
                    return 32;
            }
        }

        private void UpdateSpinnerColor()
        {
            if (UseAccentColor)
                _spinnerColor = SkinManager.ColorScheme.AccentColor;
            else
                _spinnerColor = SkinManager.ColorScheme.PrimaryColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Eğer bileşen etkin değilse, soluk renk kullan
            Color currentColor = Enabled ? _spinnerColor : SkinManager.TextDisabledOrHintColor;

            switch (_spinnerType)
            {
                case SpinnerStyle.Border:
                    DrawBorderSpinner(g, currentColor);
                    break;
                case SpinnerStyle.Grow:
                    DrawGrowSpinner(g, currentColor);
                    break;
                case SpinnerStyle.Circle:
                    DrawCircleSpinner(g, currentColor);
                    break;
                case SpinnerStyle.Dots:
                    DrawDotsSpinner(g, currentColor);
                    break;
            }
        }

        private void DrawBorderSpinner(Graphics g, Color color)
        {
            int size = GetPixelSize();
            float thickness = size * 0.1f;
            float halfSize = size / 2f;
            float radius = halfSize - thickness;

            using (Pen pen = new Pen(color, thickness))
            {
                pen.DashPattern = new float[] { 0.5f, 0.3f };
                pen.DashCap = DashCap.Round;

                // Dönen daire için matrisi ayarla
                Matrix rotationMatrix = new Matrix();
                rotationMatrix.RotateAt(_animationValue * 360, new PointF(halfSize, halfSize));
                g.Transform = rotationMatrix;

                // Daireyi çiz
                g.DrawEllipse(pen, thickness / 2, thickness / 2, size - thickness, size - thickness);
                g.ResetTransform();
            }
        }

        private void DrawGrowSpinner(Graphics g, Color color)
        {
            int size = GetPixelSize();
            float halfSize = size / 2f;
            float minRadius = size * 0.1f;
            float maxRadius = halfSize - 2;

            // Animasyon için büyüme ve küçülme
            float currentRadius = minRadius + ((float)Math.Sin(_animationValue * Math.PI) * (maxRadius - minRadius));

            using (SolidBrush brush = new SolidBrush(color))
            {
                float position = halfSize - currentRadius;
                g.FillEllipse(brush, position, position, currentRadius * 2, currentRadius * 2);
            }
        }

        private void DrawCircleSpinner(Graphics g, Color color)
        {
            int size = GetPixelSize();
            float halfSize = size / 2f;
            float thickness = size * 0.1f;
            float radius = halfSize - thickness;

            // Boş daire çiz
            using (Pen emptyPen = new Pen(Color.FromArgb(50, color), thickness))
            {
                g.DrawEllipse(emptyPen, thickness, thickness, size - thickness * 2, size - thickness * 2);
            }

            // Dönen kısmı çiz
            using (Pen filledPen = new Pen(color, thickness))
            {
                float startAngle = _animationValue * 360;
                float sweepAngle = 90;

                g.DrawArc(filledPen, thickness, thickness, size - thickness * 2, size - thickness * 2, startAngle, sweepAngle);
            }
        }

        private void DrawDotsSpinner(Graphics g, Color color)
        {
            int size = GetPixelSize();
            float halfSize = size / 2f;
            float dotRadius = size * 0.1f;
            int dotCount = 8;
            float orbitRadius = halfSize - dotRadius - 2;

            for (int i = 0; i < dotCount; i++)
            {
                float alpha = (float)(i / (double)dotCount * Math.PI * 2);

                // Yavaştan soluklaşan noktalar için alfa hesapla
                float dotAlpha = (i / (float)dotCount + _animationValue) % 1.0f;
                float opacity = 0.3f + 0.7f * (1 - dotAlpha);

                Color dotColor = Color.FromArgb((int)(opacity * 255), color);

                using (SolidBrush brush = new SolidBrush(dotColor))
                {
                    float x = halfSize + (float)Math.Cos(alpha) * orbitRadius - dotRadius;
                    float y = halfSize + (float)Math.Sin(alpha) * orbitRadius - dotRadius;

                    g.FillEllipse(brush, x, y, dotRadius * 2, dotRadius * 2);
                }
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _isAnimating = Enabled;

            if (Enabled)
                _animationTimer.Start();
            else
                _animationTimer.Stop();

            Invalidate();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            _animationTimer.Stop();
            _animationTimer.Dispose();
        }
    }
}