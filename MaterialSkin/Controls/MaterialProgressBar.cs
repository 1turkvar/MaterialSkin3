namespace MaterialSkin.Controls
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    /// <summary>
    /// Farklı stillere sahip Material tasarımında ilerleme çubuğu
    /// </summary>
    public class MaterialProgressBar : ProgressBar, IMaterialControl
    {
        private ProgressBarStyle _barStyle = ProgressBarStyle.Standard;
        private int _segments = 4;
        private int _segmentSpacing = 5;
        private int _barHeight = 5;
        private bool _vertical = false;
        private int _roundRadius = 5;
        private Color _progressColor;
        private Color _baseColor;
        private bool _useCustomProgressColor = false;
        private bool _useCustomBaseColor = false;

        /// <summary>
        /// İlerleme çubuğu stilleri
        /// </summary>
        public enum ProgressBarStyle
        {
            /// <summary>
            /// Standart yatay ilerleme çubuğu
            /// </summary>
            Standard,

            /// <summary>
            /// Yuvarlak köşelere sahip ilerleme çubuğu
            /// </summary>
            Rounded,

            /// <summary>
            /// Bölümlere ayrılmış ilerleme çubuğu
            /// </summary>
            Segmented,

            /// <summary>
            /// Dikey yönde ilerleme çubuğu
            /// </summary>
            Vertical,

            /// <summary>
            /// Mum şeklinde dikey ilerleme çubuğu
            /// </summary>
            Candle,

            /// <summary>
            /// Rulman şeklinde dairesel ilerleme çubuğu
            /// </summary>
            Circular
        }

        /// <summary>
        /// İlerleme çubuğu stilini belirler
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(ProgressBarStyle.Standard)]
        [Description("İlerleme çubuğunun görünüm stilini belirler")]
        public ProgressBarStyle BarStyle
        {
            get => _barStyle;
            set
            {
                if (_barStyle != value)
                {
                    _barStyle = value;
                    UpdateControlSize();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Bölümlü ilerleme çubuğundaki bölüm sayısı
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(4)]
        [Description("Bölümlü ilerleme çubuğundaki bölüm sayısı")]
        public int Segments
        {
            get => _segments;
            set
            {
                if (value >= 1 && _segments != value)
                {
                    _segments = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Bölümler arasındaki piksel cinsinden boşluk
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(5)]
        [Description("Bölümler arasındaki piksel cinsinden boşluk")]
        public int SegmentSpacing
        {
            get => _segmentSpacing;
            set
            {
                if (value >= 0 && _segmentSpacing != value)
                {
                    _segmentSpacing = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// İlerleme çubuğunun piksel cinsinden yüksekliği/genişliği
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(5)]
        [Description("İlerleme çubuğunun piksel cinsinden yüksekliği/genişliği")]
        public int BarHeight
        {
            get => _barHeight;
            set
            {
                if (value >= 1 && _barHeight != value)
                {
                    _barHeight = value;
                    UpdateControlSize();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Yuvarlak ilerleme çubuğunun köşe yarıçapı
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(5)]
        [Description("Yuvarlak ilerleme çubuğunun köşe yarıçapı")]
        public int RoundRadius
        {
            get => _roundRadius;
            set
            {
                if (value >= 0 && _roundRadius != value)
                {
                    _roundRadius = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// İlerleme çubuğunun tamamlanmış kısmının rengi. Null ise tema rengi kullanılır.
        /// </summary>
        [Category("Appearance")]
        [Description("İlerleme çubuğunun tamamlanmış kısmının rengi. Null ise tema rengi kullanılır.")]
        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                if (_progressColor != value)
                {
                    _progressColor = value;
                    _useCustomProgressColor = !value.IsEmpty;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// İlerleme çubuğunun tamamlanmamış kısmının rengi. Null ise tema rengi kullanılır.
        /// </summary>
        [Category("Appearance")]
        [Description("İlerleme çubuğunun tamamlanmamış kısmının rengi. Null ise tema rengi kullanılır.")]
        public Color BaseColor
        {
            get => _baseColor;
            set
            {
                if (_baseColor != value)
                {
                    _baseColor = value;
                    _useCustomBaseColor = !value.IsEmpty;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Z ekseni derinliği
        /// </summary>
        [Browsable(false)]
        public int Depth { get; set; }

        /// <summary>
        /// Material cilt yöneticisi
        /// </summary>
        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        /// <summary>
        /// Fare durumu
        /// </summary>
        [Browsable(false)]
        public MouseState MouseState { get; set; }

        /// <summary>
        /// Material ilerleme çubuğu oluşturur
        /// </summary>
        public MaterialProgressBar()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            // Varsayılan renkleri boş olarak ayarla (SkinManager renklerini kullanmak için)
            _progressColor = Color.Empty;
            _baseColor = Color.Empty;

            UpdateControlSize();
        }

        /// <summary>
        /// Stil ve boyut değişikliğine göre kontrolün boyutunu günceller
        /// </summary>
        private void UpdateControlSize()
        {
            if (_barStyle == ProgressBarStyle.Vertical || _barStyle == ProgressBarStyle.Candle)
            {
                _vertical = true;
            }
            else if (_barStyle == ProgressBarStyle.Circular)
            {
                _vertical = false;
                // Dairesel stil için minimum boyut ayarla
                MinimumSize = new Size(50, 50);
                return; // Dairesel stil için diğer boyut kısıtlamalarını uygulama
            }
            else
            {
                _vertical = false;
            }

            if (_vertical)
            {
                MinimumSize = new Size(_barHeight, 50);
                if (Width < _barHeight)
                {
                    Width = _barHeight;
                }
            }
            else
            {
                MinimumSize = new Size(50, _barHeight);
                if (Height != _barHeight && _barStyle != ProgressBarStyle.Circular)
                {
                    Height = _barHeight;
                }
            }
        }

        /// <summary>
        /// Kontrolün sınırlarını ayarlar, yüksekliği/genişliği BarHeight'a göre sabitler
        /// </summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (_barStyle == ProgressBarStyle.Circular)
            {
                // Dairesel stil için en-boy oranını koru (kare olmalı)
                int size = Math.Max(width, height);
                base.SetBoundsCore(x, y, size, size, specified);
            }
            else if (_vertical)
            {
                // Dikey modda genişlik sabit
                base.SetBoundsCore(x, y, _barHeight, height, specified);
            }
            else
            {
                // Yatay modda yükseklik sabit
                base.SetBoundsCore(x, y, width, _barHeight, specified);
            }
        }

        /// <summary>
        /// İlerleme çubuğunu çizer
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Renkleri belirle
            Brush completedBrush;
            if (_useCustomProgressColor)
            {
                completedBrush = new SolidBrush(_progressColor);
            }
            else
            {
                completedBrush = Enabled ?
                    SkinManager.ColorScheme.PrimaryBrush :
                    new SolidBrush(DrawHelper.BlendColor(SkinManager.ColorScheme.PrimaryColor, SkinManager.SwitchOffDisabledThumbColor, 197));
            }

            Brush remainingBrush;
            if (_useCustomBaseColor)
            {
                remainingBrush = new SolidBrush(_baseColor);
            }
            else
            {
                remainingBrush = SkinManager.BackgroundFocusBrush;
            }

            // Tamamlanma oranını hesapla
            double progressRatio = (double)Value / Maximum;

            // Stile göre çizim yap
            switch (_barStyle)
            {
                case ProgressBarStyle.Standard:
                    DrawStandardProgressBar(e.Graphics, progressRatio, completedBrush, remainingBrush);
                    break;

                case ProgressBarStyle.Rounded:
                    DrawRoundedProgressBar(e.Graphics, progressRatio, completedBrush, remainingBrush);
                    break;

                case ProgressBarStyle.Segmented:
                    DrawSegmentedProgressBar(e.Graphics, progressRatio, completedBrush, remainingBrush);
                    break;

                case ProgressBarStyle.Vertical:
                    DrawVerticalProgressBar(e.Graphics, progressRatio, completedBrush, remainingBrush);
                    break;

                case ProgressBarStyle.Candle:
                    DrawCandleProgressBar(e.Graphics, progressRatio, completedBrush, remainingBrush);
                    break;

                case ProgressBarStyle.Circular:
                    DrawCircularProgressBar(e.Graphics, progressRatio, completedBrush, remainingBrush);
                    break;
            }

            // SolidBrush nesneleri Dispose et (eğer özel renkler kullanılmışsa)
            if (_useCustomProgressColor)
            {
                completedBrush.Dispose();
            }

            if (_useCustomBaseColor)
            {
                remainingBrush.Dispose();
            }
        }

        /// <summary>
        /// Standart ilerleme çubuğunu çizer
        /// </summary>
        private void DrawStandardProgressBar(Graphics g, double progressRatio, Brush completedBrush, Brush remainingBrush)
        {
            int doneProgress = (int)(Width * progressRatio);

            // Tamamlanan kısım
            g.FillRectangle(completedBrush, 0, 0, doneProgress, Height);

            // Tamamlanmamış kısım
            g.FillRectangle(remainingBrush, doneProgress, 0, Width - doneProgress, Height);
        }

        /// <summary>
        /// Yuvarlak köşeli ilerleme çubuğunu çizer
        /// </summary>
        private void DrawRoundedProgressBar(Graphics g, double progressRatio, Brush completedBrush, Brush remainingBrush)
        {
            int doneProgress = (int)(Width * progressRatio);
            int radius = Math.Min(_roundRadius, Height / 2);

            // Arkaplanı çiz
            using (GraphicsPath path = CreateRoundedRectangle(0, 0, Width, Height, radius))
            {
                g.FillPath(remainingBrush, path);
            }

            // Tamamlanan kısmı çiz (eğer varsa)
            if (doneProgress > 0)
            {
                using (GraphicsPath progressPath = CreateRoundedRectangle(0, 0, doneProgress, Height, radius))
                {
                    g.FillPath(completedBrush, progressPath);
                }
            }
        }

        /// <summary>
        /// Bölümlü ilerleme çubuğunu çizer
        /// </summary>
        private void DrawSegmentedProgressBar(Graphics g, double progressRatio, Brush completedBrush, Brush remainingBrush)
        {
            // Toplam bölüm genişliğini hesapla
            int totalSegments = _segments;
            int segmentWidth = (Width - (_segmentSpacing * (totalSegments - 1))) / totalSegments;

            // Tamamlanan bölüm sayısını hesapla
            int completedSegments = (int)(totalSegments * progressRatio);

            for (int i = 0; i < totalSegments; i++)
            {
                int x = i * (segmentWidth + _segmentSpacing);
                Brush brush = (i < completedSegments) ? completedBrush : remainingBrush;

                g.FillRectangle(brush, x, 0, segmentWidth, Height);
            }
        }

        /// <summary>
        /// Dikey ilerleme çubuğunu çizer
        /// </summary>
        private void DrawVerticalProgressBar(Graphics g, double progressRatio, Brush completedBrush, Brush remainingBrush)
        {
            int totalHeight = Height;
            int doneProgress = (int)(totalHeight * progressRatio);

            // Tamamlanmamış kısım (üstte)
            g.FillRectangle(remainingBrush, 0, 0, Width, totalHeight - doneProgress);

            // Tamamlanan kısım (altta)
            g.FillRectangle(completedBrush, 0, totalHeight - doneProgress, Width, doneProgress);
        }

        /// <summary>
        /// Mum tipi dikey ilerleme çubuğunu çizer
        /// </summary>
        private void DrawCandleProgressBar(Graphics g, double progressRatio, Brush completedBrush, Brush remainingBrush)
        {
            int totalHeight = Height;
            int doneProgress = (int)(totalHeight * progressRatio);
            int centerX = Width / 2;
            int candleWidth = Width / 3;

            // Mum tabanı (ilerleme durumuna göre doluluk oranı değişir)
            if (progressRatio > 0)
            {
                // Tabanın ilerleme durumuna göre doluluk oranı
                double baseProgressRatio = Math.Min(progressRatio * 2.5, 1.0); // İlerleme %40'a gelince taban tamamen dolacak

                // Tabanın doluluk derecesini hesapla
                if (baseProgressRatio < 1.0)
                {
                    // Kısmi dolu taban
                    using (GraphicsPath basePath = new GraphicsPath())
                    {
                        // Dolu kısım için açı hesapla (0-360 arasında)
                        float startAngle = -90; // Üstten başla
                        float sweepAngle = (float)(baseProgressRatio * 360);

                        Rectangle baseRect = new Rectangle(
                            centerX - candleWidth / 2,
                            totalHeight - candleWidth,
                            candleWidth,
                            candleWidth);

                        // Dolu kısım
                        g.FillPie(completedBrush, baseRect, startAngle, sweepAngle);

                        // Boş kısım
                        g.FillPie(remainingBrush, baseRect, startAngle + sweepAngle, 360 - sweepAngle);
                    }
                }
                else
                {
                    // Tamamen dolu taban
                    g.FillEllipse(completedBrush, centerX - candleWidth / 2, totalHeight - candleWidth, candleWidth, candleWidth);
                }
            }
            else
            {
                // İlerleme sıfırsa taban tamamen boş
                g.FillEllipse(remainingBrush, centerX - candleWidth / 2, totalHeight - candleWidth, candleWidth, candleWidth);
            }

            // Mum gövdesi
            // Tamamlanmamış kısım (üstte)
            g.FillRectangle(remainingBrush, centerX - candleWidth / 4, 0, candleWidth / 2, totalHeight - doneProgress);

            // Tamamlanan kısım (altta)
            if (doneProgress > candleWidth / 2)
            {
                g.FillRectangle(completedBrush, centerX - candleWidth / 4, totalHeight - doneProgress, candleWidth / 2, doneProgress - candleWidth / 2);
            }

            // Mum fitili
            // Fitil için özelleştirilebilir gri renk (sabit koyu gri yerine)
            Color wickColor = Color.DarkGray;
            // Eğer özel tamamlanmamış renk kullanılıyorsa, fitil için de daha uygun bir renk seç
            if (_useCustomBaseColor)
            {
                // Fitil rengini, tamamlanmamış kısmın rengine göre daha koyu bir ton yap
                wickColor = ControlPaint.Dark(_baseColor, 0.3f);
            }

            g.FillRectangle(new SolidBrush(wickColor), centerX - 1, 0, 2, 10);
        }

        /// <summary>
        /// Dairesel rulman tipinde ilerleme çubuğunu çizer
        /// </summary>
        private void DrawCircularProgressBar(Graphics g, double progressRatio, Brush completedBrush, Brush remainingBrush)
        {
            // Kontrol boyutlarını al, en-boy oranı korunarak kare bir alan oluştur
            int size = Math.Min(Width, Height);
            int x = (Width - size) / 2;
            int y = (Height - size) / 2;

            // Rulman kalınlığı (toplam boyutun %15'i)
            int thickness = (int)(size * 0.15);

            // Dış ve iç daire için dikdörtgenler
            RectangleF outerRect = new RectangleF(x, y, size, size);
            RectangleF innerRect = new RectangleF(
                x + thickness,
                y + thickness,
                size - 2 * thickness,
                size - 2 * thickness);

            // Başlangıç açısı (üstten başla)
            float startAngle = -90;

            // İlerleme açısı (0-360 arası)
            float sweepAngle = (float)(progressRatio * 360);

            // Arkaplan (tamamlanmamış kısım) - tam daire
            using (GraphicsPath outerPath = new GraphicsPath())
            {
                // Dış daire
                outerPath.AddEllipse(outerRect);

                // İç daire (delik)
                outerPath.AddEllipse(innerRect);

                // Deliği çıkart
                outerPath.SetMarkers();

                // Çiz
                g.FillPath(remainingBrush, outerPath);
            }

            // İlerleme varsa tamamlanan kısmı çiz
            if (progressRatio > 0)
            {
                // Tamamlanan kısım için yay çiz
                using (GraphicsPath progressPath = new GraphicsPath())
                {
                    // Dış yay
                    progressPath.AddArc(outerRect, startAngle, sweepAngle);

                    // İç daire kenarına bağla
                    PointF innerEndPoint = CalculatePointOnCircle(
                        innerRect.X + innerRect.Width / 2,
                        innerRect.Y + innerRect.Height / 2,
                        innerRect.Width / 2,
                        startAngle + sweepAngle);

                    progressPath.AddLine(
                        progressPath.GetLastPoint(),
                        innerEndPoint);

                    // İç yay (ters yönde)
                    progressPath.AddArc(innerRect, startAngle + sweepAngle, -sweepAngle);

                    // Başlangıç noktasına bağla
                    progressPath.CloseAllFigures();

                    // Çiz
                    g.FillPath(completedBrush, progressPath);
                }
            }
        }

        /// <summary>
        /// Daire üzerinde belirli bir açıdaki noktayı hesaplar
        /// </summary>
        private PointF CalculatePointOnCircle(float centerX, float centerY, float radius, float angleDegrees)
        {
            // Açıyı radyana çevir
            double angleRadians = angleDegrees * Math.PI / 180.0;

            // X ve Y koordinatlarını hesapla
            float x = centerX + (float)(radius * Math.Cos(angleRadians));
            float y = centerY + (float)(radius * Math.Sin(angleRadians));

            return new PointF(x, y);
        }

        /// <summary>
        /// Yuvarlak köşeli dikdörtgen oluşturur
        /// </summary>
        private GraphicsPath CreateRoundedRectangle(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            // Sol üst köşe
            path.AddArc(x, y, 2 * radius, 2 * radius, 180, 90);

            // Üst kenar
            path.AddLine(x + radius, y, x + width - radius, y);

            // Sağ üst köşe
            path.AddArc(x + width - 2 * radius, y, 2 * radius, 2 * radius, 270, 90);

            // Sağ kenar
            path.AddLine(x + width, y + radius, x + width, y + height - radius);

            // Sağ alt köşe
            path.AddArc(x + width - 2 * radius, y + height - 2 * radius, 2 * radius, 2 * radius, 0, 90);

            // Alt kenar
            path.AddLine(x + width - radius, y + height, x + radius, y + height);

            // Sol alt köşe
            path.AddArc(x, y + height - 2 * radius, 2 * radius, 2 * radius, 90, 90);

            // Sol kenar
            path.AddLine(x, y + height - radius, x, y + radius);

            path.CloseFigure();
            return path;
        }
    }
}