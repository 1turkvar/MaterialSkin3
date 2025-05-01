namespace MaterialSkin.Controls
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Material tasarım stilinde özelleştirilmiş ListView kontrolü.
    /// </summary>
    public class MaterialListView : ListView, IMaterialControl
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
                    if (value && Visible)
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

        [Browsable(false)]
        private ListViewItem HoveredItem { get; set; }

        #endregion

        #region Constructor

        public MaterialListView()
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

        #region Drawing Methods

        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            using (Graphics g = e.Graphics)
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                g.FillRectangle(new SolidBrush(BackColor), e.Bounds);

                // Draw Text
                using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                {
                    nativeText.DrawTransparentText(
                        e.Header.Text,
                        SkinManager.getLogFontByType(MaterialSkinManager.fontType.Subtitle2),
                        Enabled ? SkinManager.TextHighEmphasisNoAlphaColor : SkinManager.TextDisabledOrHintColor,
                        new Point(e.Bounds.Location.X + PAD, e.Bounds.Location.Y),
                        new Size(e.Bounds.Size.Width - PAD * 2, e.Bounds.Size.Height),
                        NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                }
            }
        }

        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            using (Graphics g = e.Graphics)
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Always draw default background
                g.FillRectangle(SkinManager.BackgroundBrush, e.Bounds);

                if (e.Item.Selected)
                {
                    // Selected background
                    g.FillRectangle(SkinManager.BackgroundFocusBrush, e.Bounds);
                }
                else if (e.Bounds.Contains(MouseLocation) && MouseState == MouseState.HOVER)
                {
                    // Hover background
                    g.FillRectangle(SkinManager.BackgroundHoverBrush, e.Bounds);
                }

                // Draw separator line
                using (Pen dividerPen = new Pen(SkinManager.DividersColor))
                {
                    g.DrawLine(dividerPen, e.Bounds.Left, e.Bounds.Y, e.Bounds.Right, e.Bounds.Y);
                }

                foreach (ListViewItem.ListViewSubItem subItem in e.Item.SubItems)
                {
                    // Draw Text
                    using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                    {
                        nativeText.DrawTransparentText(
                            subItem.Text,
                            SkinManager.getLogFontByType(MaterialSkinManager.fontType.Body2),
                            Enabled ? SkinManager.TextHighEmphasisNoAlphaColor : SkinManager.TextDisabledOrHintColor,
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
                SafeBeginInvoke(AutoResize);
            }
        }

        protected override void OnColumnWidthChanged(ColumnWidthChangedEventArgs e)
        {
            base.OnColumnWidthChanged(e);
            if (AutoSizeTable && IsHandleCreated)
            {
                SafeBeginInvoke(AutoResize);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (AutoSizeTable && IsHandleCreated)
            {
                SafeBeginInvoke(AutoResize);
            }
        }

        private void AutoResize()
        {
            if (!AutoSizeTable || !IsHandleCreated) return;

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

            if (AutoSizeTable && IsHandleCreated)
            {
                SafeBeginInvoke(AutoResize);
            }
        }

        /// <summary>
        /// Handle'ın oluşturulduğundan emin olarak BeginInvoke çağrısı yapar
        /// </summary>
        private void SafeBeginInvoke(Action action)
        {
            try
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(action);
                }
                else
                {
                    // Handle oluşturulmamış, action'ı doğrudan çağırabiliriz veya hiçbir şey yapmayabiliriz
                    action();
                }
            }
            catch (InvalidOperationException)
            {
                // Handle oluşturulmadan BeginInvoke çağrılmışsa, doğrudan action'ı çağır
                action();
            }
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            BackColor = SkinManager.BackgroundColor;
        }

        #endregion

        #region Disposal

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                DetachEventHandlers();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}