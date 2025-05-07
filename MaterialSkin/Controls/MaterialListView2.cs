    namespace MaterialSkin.Controls
    {
        using System;
        using System.ComponentModel;
        using System.Drawing;
        using System.Windows.Forms;
        using System.Collections.Generic;

        /// <summary>
        /// Material tasarım stilinde özelleştirilmiş, Bootstrap tablo stillerini destekleyen ListView kontrolü.
        /// </summary>
        public class MaterialListView2 : ListView, IMaterialControl
        {
            #region Interface Implementation

            [Browsable(false)]
            public int Depth { get; set; }

            [Browsable(false)]
            public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

            [Browsable(false)]
            public MouseState MouseState { get; set; }

            [Browsable(false)]
            public Point MouseLocation { get; set; }

            #endregion

            #region Private Fields

            private const int PAD = 16;
            private const int ITEMS_HEIGHT = 52;
            private bool _autoSizeTable;
            private bool _disposed;
            private TableStyle _tableStyle = TableStyle.Default;
            private bool _useZebraStripes = false;
            private bool _useBorders = true;
            private bool _useHoverEffect = true;
            private bool _useSelectedEffect = true;
            private Color _customHeaderBackColor = Color.Empty;
            private Color _customHeaderTextColor = Color.Empty;
            private Color _customRowBackColor = Color.Empty;
            private Color _customRowAltBackColor = Color.Empty;
            private Color _customRowTextColor = Color.Empty;
            private Color _customBorderColor = Color.Empty;
            private Color _customHoverColor = Color.Empty;
            private Color _customSelectedColor = Color.Empty;
            private int _rowHeight = ITEMS_HEIGHT;

            // Önbellek değişkenleri
            private Color _cachedHeaderBackColor;
            private Color _cachedBorderColor;
            private Color _cachedSelectedColor;
            private bool _colorCacheValid = false;

            // Ortak kullanılan fırça ve kalemler
            private SolidBrush _headerBackBrush;
            private SolidBrush _rowBackBrush;
            private SolidBrush _altRowBackBrush;
            private SolidBrush _selectedBrush;
            private SolidBrush _hoverBrush;
            private Pen _borderPen;

            #endregion

            #region Enums and Types

            /// <summary>
            /// Tablo stillerini temsil eden enum
            /// </summary>
            public enum TableStyle
            {
                Default,
                Primary,
                Secondary,
                Success,
                Danger,
                Warning,
                Info,
                Light,
                Dark,
                Custom
            }

            #endregion

            #region Properties

            [Category("Appearance"), Browsable(true)]
            public bool AutoSizeTable
            {
                get => _autoSizeTable;
                set
                {
                    if (_autoSizeTable != value)
                    {
                        _autoSizeTable = value;
                        Scrollable = !value;

                        // Sadece AutoSize aktifleştirilmiş ve kontrol görünür durumdaysa resize işlemini yap
                        if (value && Visible && IsHandleCreated)
                        {
                            try
                            {
                                AutoResize();
                            }
                            catch (InvalidOperationException)
                            {
                                // Handle henüz oluşturulmamış olabilir, görmezden gel
                            }
                        }
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(TableStyle.Default)]
            public TableStyle BootstrapStyle
            {
                get => _tableStyle;
                set
                {
                    if (_tableStyle != value)
                    {
                        _tableStyle = value;
                        _colorCacheValid = false;
                        UpdateBrushesAndPens();
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(false)]
            public bool UseZebraStripes
            {
                get => _useZebraStripes;
                set
                {
                    if (_useZebraStripes != value)
                    {
                        _useZebraStripes = value;
                        UpdateBrushesAndPens();
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(true)]
            public bool UseBorders
            {
                get => _useBorders;
                set
                {
                    if (_useBorders != value)
                    {
                        _useBorders = value;
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(true)]
            public bool UseHoverEffect
            {
                get => _useHoverEffect;
                set
                {
                    if (_useHoverEffect != value)
                    {
                        _useHoverEffect = value;
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(true)]
            public bool UseSelectedEffect
            {
                get => _useSelectedEffect;
                set
                {
                    if (_useSelectedEffect != value)
                    {
                        _useSelectedEffect = value;
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(typeof(Color), "Empty")]
            public Color CustomHeaderBackColor
            {
                get => _customHeaderBackColor;
                set
                {
                    if (_customHeaderBackColor != value)
                    {
                        _customHeaderBackColor = value;
                        _colorCacheValid = false;
                        UpdateBrushesAndPens();
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(typeof(Color), "Empty")]
            public Color CustomHeaderTextColor
            {
                get => _customHeaderTextColor;
                set
                {
                    if (_customHeaderTextColor != value)
                    {
                        _customHeaderTextColor = value;
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(typeof(Color), "Empty")]
            public Color CustomRowBackColor
            {
                get => _customRowBackColor;
                set
                {
                    if (_customRowBackColor != value)
                    {
                        _customRowBackColor = value;
                        UpdateBrushesAndPens();
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(typeof(Color), "Empty")]
            public Color CustomRowAltBackColor
            {
                get => _customRowAltBackColor;
                set
                {
                    if (_customRowAltBackColor != value)
                    {
                        _customRowAltBackColor = value;
                        UpdateBrushesAndPens();
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(typeof(Color), "Empty")]
            public Color CustomRowTextColor
            {
                get => _customRowTextColor;
                set
                {
                    if (_customRowTextColor != value)
                    {
                        _customRowTextColor = value;
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(typeof(Color), "Empty")]
            public Color CustomBorderColor
            {
                get => _customBorderColor;
                set
                {
                    if (_customBorderColor != value)
                    {
                        _customBorderColor = value;
                        _colorCacheValid = false;
                        UpdateBrushesAndPens();
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(typeof(Color), "Empty")]
            public Color CustomHoverColor
            {
                get => _customHoverColor;
                set
                {
                    if (_customHoverColor != value)
                    {
                        _customHoverColor = value;
                        UpdateBrushesAndPens();
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(typeof(Color), "Empty")]
            public Color CustomSelectedColor
            {
                get => _customSelectedColor;
                set
                {
                    if (_customSelectedColor != value)
                    {
                        _customSelectedColor = value;
                        UpdateBrushesAndPens();
                        Invalidate();
                    }
                }
            }

            [Category("Appearance"), Browsable(true), DefaultValue(ITEMS_HEIGHT)]
            public int RowHeight
            {
                get => _rowHeight;
                set
                {
                    if (_rowHeight != value)
                    {
                        _rowHeight = value;
                        SetItemHeight();
                        Invalidate();
                    }
                }
            }

            [Browsable(false)]
            private ListViewItem HoveredItem { get; set; }

            #endregion

            #region Constructor

            public MaterialListView2()
            {
                GridLines = false;
                FullRowSelect = true;
                View = View.Details;
                OwnerDraw = true;
                ResizeRedraw = true;
                BorderStyle = BorderStyle.None;
                MinimumSize = new Size(200, 100);

                SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);

                // Avoid flickering
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);

                BackColor = SkinManager.BackgroundColor;

                // Fix for hovers, by default it doesn't redraw
                MouseLocation = new Point(-1, -1);
                MouseState = MouseState.OUT;

                // Fırça ve kalemleri başlat
                InitializeBrushesAndPens();

                AttachEventHandlers();
            }

            #endregion

            #region Event Handlers

            private void AttachEventHandlers()
            {
                MouseEnter += OnMouseEnterInternal;
                MouseLeave += OnMouseLeaveInternal;
                MouseDown += OnMouseDownInternal;
                MouseUp += OnMouseUpInternal;
                MouseMove += OnMouseMoveInternal;
            }

            private void DetachEventHandlers()
            {
                MouseEnter -= OnMouseEnterInternal;
                MouseLeave -= OnMouseLeaveInternal;
                MouseDown -= OnMouseDownInternal;
                MouseUp -= OnMouseUpInternal;
                MouseMove -= OnMouseMoveInternal;
            }

            private void OnMouseEnterInternal(object sender, EventArgs e)
            {
                MouseState = MouseState.HOVER;
            }

            private void OnMouseLeaveInternal(object sender, EventArgs e)
            {
                MouseState = MouseState.OUT;
                MouseLocation = new Point(-1, -1);
                HoveredItem = null;
                Invalidate();
            }

            private void OnMouseDownInternal(object sender, EventArgs e)
            {
                MouseState = MouseState.DOWN;
            }

            private void OnMouseUpInternal(object sender, EventArgs e)
            {
                MouseState = MouseState.HOVER;
            }

            private void OnMouseMoveInternal(object sender, MouseEventArgs args)
            {
                MouseLocation = args.Location;
                var currentHoveredItem = GetItemAt(MouseLocation.X, MouseLocation.Y);
                if (HoveredItem != currentHoveredItem)
                {
                    HoveredItem = currentHoveredItem;
                    Invalidate();
                }
            }

            #endregion

            #region Style Methods

            /// <summary>
            /// Fırçaları ve kalemleri başlatır
            /// </summary>
            private void InitializeBrushesAndPens()
            {
                _headerBackBrush = new SolidBrush(GetHeaderBackColor());
                _rowBackBrush = new SolidBrush(SkinManager.BackgroundColor);
                _altRowBackBrush = new SolidBrush(Color.FromArgb(30, GetHeaderBackColor()));
                _selectedBrush = new SolidBrush(GetSelectedColor());
                _hoverBrush = new SolidBrush(GetHoverColor());
                _borderPen = new Pen(GetBorderColor());
            }

            /// <summary>
            /// Fırçaları ve kalemleri günceller
            /// </summary>
            private void UpdateBrushesAndPens()
            {
                _headerBackBrush?.Dispose();
                _rowBackBrush?.Dispose();
                _altRowBackBrush?.Dispose();
                _selectedBrush?.Dispose();
                _hoverBrush?.Dispose();
                _borderPen?.Dispose();

                InitializeBrushesAndPens();
            }

            /// <summary>
            /// Seçilen Bootstrap stiline göre başlık arka plan rengini döndürür
            /// </summary>
            private Color GetHeaderBackColor()
            {
                if (_colorCacheValid)
                {
                    return _cachedHeaderBackColor;
                }

                Color result;
                if (_tableStyle == TableStyle.Custom && _customHeaderBackColor != Color.Empty)
                    result = _customHeaderBackColor;
                else
                {
                    switch (_tableStyle)
                    {
                        case TableStyle.Primary:
                            result = Color.FromArgb(13, 110, 253);    // Bootstrap Primary
                            break;
                        case TableStyle.Secondary:
                            result = Color.FromArgb(108, 117, 125);   // Bootstrap Secondary
                            break;
                        case TableStyle.Success:
                            result = Color.FromArgb(25, 135, 84);     // Bootstrap Success
                            break;
                        case TableStyle.Danger:
                            result = Color.FromArgb(220, 53, 69);     // Bootstrap Danger
                            break;
                        case TableStyle.Warning:
                            result = Color.FromArgb(255, 193, 7);     // Bootstrap Warning
                            break;
                        case TableStyle.Info:
                            result = Color.FromArgb(13, 202, 240);    // Bootstrap Info
                            break;
                        case TableStyle.Light:
                            result = Color.FromArgb(248, 249, 250);   // Bootstrap Light
                            break;
                        case TableStyle.Dark:
                            result = Color.FromArgb(33, 37, 41);      // Bootstrap Dark
                            break;
                        case TableStyle.Default:
                        default:
                            result = SkinManager.ColorScheme.PrimaryColor;
                            break;
                    }
                }

                _cachedHeaderBackColor = result;
                return result;
            }

            /// <summary>
            /// Seçilen Bootstrap stiline göre başlık metin rengini döndürür
            /// </summary>
            private Color GetHeaderTextColor()
            {
                if (_tableStyle == TableStyle.Custom && _customHeaderTextColor != Color.Empty)
                    return _customHeaderTextColor;

                // Koyu arka plan renkleri için beyaz metin
                if (_tableStyle == TableStyle.Primary ||
                    _tableStyle == TableStyle.Secondary ||
                    _tableStyle == TableStyle.Success ||
                    _tableStyle == TableStyle.Danger ||
                    _tableStyle == TableStyle.Dark)
                {
                    return Color.White;
                }
                // Açık arka plan renkleri için koyu metin
                else if (_tableStyle == TableStyle.Warning ||
                         _tableStyle == TableStyle.Info ||
                         _tableStyle == TableStyle.Light)
                {
                    return Color.FromArgb(33, 37, 41);  // Bootstrap Dark
                }
                // Default
                else
                {
                    return Color.White;
                }
            }

            /// <summary>
            /// Satır arka plan rengini döndürür (zebra çizgili görünüm için)
            /// </summary>
            private Color GetRowBackColor(int rowIndex)
            {
                // Özel renk tanımlanmışsa
                if (_tableStyle == TableStyle.Custom)
                {
                    if (rowIndex % 2 == 0)
                    {
                        return _customRowBackColor != Color.Empty ? _customRowBackColor : SkinManager.BackgroundColor;
                    }
                    else
                    {
                        return _useZebraStripes && _customRowAltBackColor != Color.Empty ?
                               _customRowAltBackColor :
                               (_customRowBackColor != Color.Empty ? _customRowBackColor : SkinManager.BackgroundColor);
                    }
                }

                // Normal satır rengi
                if (rowIndex % 2 == 0 || !_useZebraStripes)
                {
                    return SkinManager.BackgroundColor;
                }
                // Alternatif satır rengi (zebra çizgili görünüm için)
                else
                {
                    Color baseColor = GetHeaderBackColor();
                    // Ana rengin daha açık versiyonu
                    return Color.FromArgb(30, baseColor);
                }
            }

            /// <summary>
            /// Satır metin rengini döndürür
            /// </summary>
            private Color GetRowTextColor()
            {
                if (_tableStyle == TableStyle.Custom && _customRowTextColor != Color.Empty)
                    return _customRowTextColor;

                return Enabled ? SkinManager.TextHighEmphasisNoAlphaColor : SkinManager.TextDisabledOrHintColor;
            }

            /// <summary>
            /// Kenarlık rengini döndürür
            /// </summary>
            private Color GetBorderColor()
            {
                if (_colorCacheValid)
                {
                    return _cachedBorderColor;
                }

                Color result;
                if (_tableStyle == TableStyle.Custom && _customBorderColor != Color.Empty)
                    result = _customBorderColor;
                else
                {
                    switch (_tableStyle)
                    {
                        case TableStyle.Primary:
                        case TableStyle.Secondary:
                        case TableStyle.Success:
                        case TableStyle.Danger:
                        case TableStyle.Warning:
                        case TableStyle.Info:
                        case TableStyle.Light:
                        case TableStyle.Dark:
                            // Tema renginin daha açık tonu
                            Color baseColor = _cachedHeaderBackColor; // Önbellek kullan
                            result = Color.FromArgb(100, baseColor);
                            break;
                        case TableStyle.Default:
                        default:
                            result = SkinManager.DividersColor;
                            break;
                    }
                }

                _cachedBorderColor = result;
                return result;
            }

            /// <summary>
            /// Hover rengini döndürür
            /// </summary>
            private Color GetHoverColor()
            {
                if (_tableStyle == TableStyle.Custom && _customHoverColor != Color.Empty)
                    return _customHoverColor;

                // Temaya göre varsayılan hover rengi
                return SkinManager.BackgroundHoverColor;
            }

            /// <summary>
            /// Seçili satır rengini döndürür
            /// </summary>
            private Color GetSelectedColor()
            {
                if (_colorCacheValid)
                {
                    return _cachedSelectedColor;
                }

                Color result;
                if (_tableStyle == TableStyle.Custom && _customSelectedColor != Color.Empty)
                    result = _customSelectedColor;
                else
                {
                    // Tema renginin daha yoğun tonu
                    Color baseColor = _cachedHeaderBackColor; // Önbellek kullan
                    result = Color.FromArgb(70, baseColor);
                }

                _cachedSelectedColor = result;
                return result;
            }

            #endregion

            #region Drawing Methods

            protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
            {
                using (Graphics g = e.Graphics)
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Başlık arka planı
                    g.FillRectangle(_headerBackBrush, e.Bounds);

                    // Başlık metni
                    using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                    {
                        nativeText.DrawTransparentText(
                            e.Header.Text,
                            SkinManager.getLogFontByType(MaterialSkinManager.fontType.Subtitle2),
                            GetHeaderTextColor(),
                            new Point(e.Bounds.Location.X + PAD, e.Bounds.Location.Y),
                            new Size(e.Bounds.Size.Width - PAD * 2, e.Bounds.Size.Height),
                            NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                    }

                    // Başlık alt kenarlığı
                    if (_useBorders)
                    {
                        g.DrawLine(_borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    }
                }
            }

            protected override void OnDrawItem(DrawListViewItemEventArgs e)
            {
                using (Graphics g = e.Graphics)
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Satır arka plan rengi (zebra çizgili veya normal)
                    int itemIndex = e.ItemIndex;
                    SolidBrush rowBrush;

                    if (_useZebraStripes && itemIndex % 2 != 0)
                    {
                        rowBrush = _altRowBackBrush;
                    }
                    else
                    {
                        rowBrush = _rowBackBrush;
                    }

                    g.FillRectangle(rowBrush, e.Bounds);

                    // Seçili veya hover durumunda arka plan değişikliği
                    if (e.Item.Selected && _useSelectedEffect)
                    {
                        // Seçili arka plan
                        g.FillRectangle(_selectedBrush, e.Bounds);
                    }
                    else if (e.Bounds.Contains(MouseLocation) && MouseState == MouseState.HOVER && _useHoverEffect)
                    {
                        // Hover arka plan
                        g.FillRectangle(_hoverBrush, e.Bounds);
                    }

                    // Satır ayırıcı çizgisi
                    if (_useBorders)
                    {
                        g.DrawLine(_borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

                        // Dikey ayırıcılar (opsiyonel)
                        int xPos = 0;
                        foreach (ColumnHeader column in Columns)
                        {
                            xPos += column.Width;
                            if (xPos < e.Bounds.Right)
                            {
                                g.DrawLine(_borderPen, xPos, e.Bounds.Top, xPos, e.Bounds.Bottom);
                            }
                        }
                    }

                    // Alt öğeleri çiz
                    foreach (ListViewItem.ListViewSubItem subItem in e.Item.SubItems)
                    {
                        // Metin çizimi
                        using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                        {
                            nativeText.DrawTransparentText(
                                subItem.Text,
                                SkinManager.getLogFontByType(MaterialSkinManager.fontType.Body2),
                                GetRowTextColor(),
                                new Point(subItem.Bounds.X + PAD, subItem.Bounds.Y),
                                new Size(subItem.Bounds.Width - PAD * 2, subItem.Bounds.Height),
                                NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                        }
                    }
                }
            }

            #endregion

            #region Size and Layout

            // Resize event handlers
            protected override void OnColumnWidthChanging(ColumnWidthChangingEventArgs e)
            {
                base.OnColumnWidthChanging(e);
                if (AutoSizeTable && IsHandleCreated)
                {
                    BeginInvoke(new Action(AutoResize));
                }
            }

            protected override void OnColumnWidthChanged(ColumnWidthChangedEventArgs e)
            {
                base.OnColumnWidthChanged(e);
                if (AutoSizeTable && IsHandleCreated)
                {
                    BeginInvoke(new Action(AutoResize));
                }
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                if (AutoSizeTable && IsHandleCreated)
                {
                    BeginInvoke(new Action(AutoResize));
                }
            }

            private void AutoResize()
            {
                if (!AutoSizeTable || !IsHandleCreated || IsDisposed) return;

                try
                {
                    // Width
                    int w = 0;
                    foreach (ColumnHeader col in Columns)
                    {
                        w += col.Width;
                    }

                    // Height
                    int h = 50; // Header size
                    if (Items.Count > 0 && TopItem != null)
                    {
                        h = TopItem.Bounds.Top;
                        foreach (ListViewItem item in Items)
                        {
                            h += item.Bounds.Height;
                        }
                    }

                    Size = new Size(w, h);
                }
                catch (ObjectDisposedException)
                {
                    // Handle case where control is being disposed
                }
            }

            #endregion

            #region Control Initialization and Overrides

            /// <summary>
            /// Satır yüksekliğini ayarlar
            /// </summary>
            private void SetItemHeight()
            {
                if (Items.Count > 0)
                {
                    // ListView'da öğelerin boyutunu özel olarak ayarlamak için
                    ImageList imgList = new ImageList();
                    imgList.ImageSize = new Size(1, RowHeight);
                    SmallImageList = imgList;
                }
            }

            protected override void InitLayout()
            {
                base.InitLayout();

                // enforce settings
                GridLines = false;
                FullRowSelect = true;
                View = View.Details;
                OwnerDraw = true;
                ResizeRedraw = true;
                BorderStyle = BorderStyle.None;
            }

            protected override void OnCreateControl()
            {
                base.OnCreateControl();

                // Satır yüksekliğini ayarla
                SetItemHeight();

                if (AutoSizeTable && IsHandleCreated)
                {
                    BeginInvoke(new Action(AutoResize));
                }
            }

            protected override void OnBackColorChanged(EventArgs e)
            {
                base.OnBackColorChanged(e);
                BackColor = SkinManager.BackgroundColor;

                // BackColor değişirse, fırçaları güncelle
                UpdateBrushesAndPens();
            }

            #endregion

            #region Helper Methods

            /// <summary>
            /// Tüm özelleştirmeleri tek metodla ayarlamaya yardımcı olur
            /// </summary>
            public void SetBootstrapStyle(TableStyle style, bool useZebraStripes = false, bool useBorders = true,
                                          bool useHoverEffect = true, bool useSelectedEffect = true)
            {
                BootstrapStyle = style;
                UseZebraStripes = useZebraStripes;
                UseBorders = useBorders;
                UseHoverEffect = useHoverEffect;
                UseSelectedEffect = useSelectedEffect;
            }

            /// <summary>
            /// Custom stil için tüm renkleri tek metodla ayarlamaya yardımcı olur
            /// </summary>
            public void SetCustomColors(Color headerBack, Color headerText, Color rowBack, Color rowAlt,
                                       Color rowText, Color border, Color hover, Color selected)
            {
                SuspendLayout();
                try
                {
                    BootstrapStyle = TableStyle.Custom;
                    CustomHeaderBackColor = headerBack;
                    CustomHeaderTextColor = headerText;
                    CustomRowBackColor = rowBack;
                    CustomRowAltBackColor = rowAlt;
                    CustomRowTextColor = rowText;
                    CustomBorderColor = border;
                    CustomHoverColor = hover;
                    CustomSelectedColor = selected;
                }
                finally
                {
                    ResumeLayout();
                }
            }

            #endregion

            #region Disposal

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_disposed)
                {
                    _disposed = true;
                    DetachEventHandlers();

                    // Fırça ve kalemleri temizle
                    _headerBackBrush?.Dispose();
                    _rowBackBrush?.Dispose();
                    _altRowBackBrush?.Dispose();
                    _selectedBrush?.Dispose();
                    _hoverBrush?.Dispose();
                    _borderPen?.Dispose();
                }
                base.Dispose(disposing);
            }

            #endregion
        }
    }