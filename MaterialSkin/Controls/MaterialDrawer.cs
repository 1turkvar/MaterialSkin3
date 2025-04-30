namespace MaterialSkin.Controls
{
    using MaterialSkin.Animations;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.Windows.Forms;

    public class MaterialDrawer : Control, IMaterialControl
    {
        // Private fields
        private bool _showIconsWhenHidden;
        private bool _isOpen;
        private bool _useColors;
        private bool _highlightWithAccent;
        private bool _backgroundWithAccent;
        private int _previousSelectedTabIndex;
        private Point _animationSource;
        private readonly AnimationManager _clickAnimManager;
        private readonly AnimationManager _showHideAnimManager;
        private List<Rectangle> _drawerItemRects;
        private List<GraphicsPath> _drawerItemPaths;
        private int _drawerItemHeight;
        private int _prevLocation;
        private int _lastMouseY;
        private int _lastLocationY;
        private int _rippleSize;
        private MaterialTabControl _baseTabControl;

        // Dictionary collections for icons
        private Dictionary<string, TextureBrush> _iconsBrushes;
        private Dictionary<string, TextureBrush> _iconsSelectedBrushes;
        private Dictionary<string, Rectangle> _iconsSize;

        // Constants
        private const int TAB_HEADER_PADDING = 24;
        private const int BORDER_WIDTH = 7;

        [Category("Drawer")]
        public bool ShowIconsWhenHidden
        {
            get => _showIconsWhenHidden;
            set
            {
                if (_showIconsWhenHidden != value)
                {
                    _showIconsWhenHidden = value;
                    UpdateTabRects();
                    PreProcessIcons();
                    ShowHideAnimation();
                    using (PaintEventArgs args = new PaintEventArgs(CreateGraphics(), ClientRectangle))
                    {
                        Paint(args);
                    }
                    DrawerShowIconsWhenHiddenChanged?.Invoke(this);
                }
            }
        }

        [Category("Drawer")]
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                _isOpen = value;
                if (value)
                    Show();
                else
                    Hide();
            }
        }

        [Category("Drawer")]
        public bool AutoHide { get; set; }

        [Category("Drawer")]
        public bool AutoShow { get; set; }

        [Category("Drawer")]
        public bool UseColors
        {
            get => _useColors;
            set
            {
                _useColors = value;
                PreProcessIcons();
                Invalidate();
            }
        }

        [Category("Drawer")]
        public bool HighlightWithAccent
        {
            get => _highlightWithAccent;
            set
            {
                _highlightWithAccent = value;
                PreProcessIcons();
                Invalidate();
            }
        }

        [Category("Drawer")]
        public bool BackgroundWithAccent
        {
            get => _backgroundWithAccent;
            set
            {
                _backgroundWithAccent = value;
                Invalidate();
            }
        }

        [Category("Drawer")]
        public int IndicatorWidth { get; set; }

        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        public int MinWidth { get; private set; }

        // Events
        public delegate void DrawerStateHandler(object sender);

        public event DrawerStateHandler DrawerStateChanged;
        public event DrawerStateHandler DrawerBeginOpen;
        public event DrawerStateHandler DrawerEndOpen;
        public event DrawerStateHandler DrawerBeginClose;
        public event DrawerStateHandler DrawerEndClose;
        public event DrawerStateHandler DrawerShowIconsWhenHiddenChanged;
        public event EventHandler<Cursor> CursorUpdate;

        [Category("Behavior")]
        public MaterialTabControl BaseTabControl
        {
            get => _baseTabControl;
            set
            {
                if (_baseTabControl != value)
                {
                    // Unsubscribe from previous events if there was a previous control
                    if (_baseTabControl != null)
                    {
                        _baseTabControl.Deselected -= BaseTabControl_Deselected;
                        _baseTabControl.SelectedIndexChanged -= BaseTabControl_SelectedIndexChanged;
                        _baseTabControl.ControlAdded -= BaseTabControl_ControlChanged;
                        _baseTabControl.ControlRemoved -= BaseTabControl_ControlChanged;
                    }

                    _baseTabControl = value;

                    if (_baseTabControl == null)
                        return;

                    UpdateTabRects();
                    PreProcessIcons();

                    // Subscribe to events
                    _previousSelectedTabIndex = _baseTabControl.SelectedIndex;
                    _baseTabControl.Deselected += BaseTabControl_Deselected;
                    _baseTabControl.SelectedIndexChanged += BaseTabControl_SelectedIndexChanged;
                    _baseTabControl.ControlAdded += BaseTabControl_ControlChanged;
                    _baseTabControl.ControlRemoved += BaseTabControl_ControlChanged;
                }
            }
        }

        private void BaseTabControl_Deselected(object sender, EventArgs e)
        {
            _previousSelectedTabIndex = _baseTabControl.SelectedIndex;
        }

        private void BaseTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            _clickAnimManager.SetProgress(0);
            _clickAnimManager.StartNewAnimation(AnimationDirection.In);
        }

        private void BaseTabControl_ControlChanged(object sender, ControlEventArgs e)
        {
            Invalidate();
        }

        private void PreProcessIcons()
        {
            // Pre-process and pre-allocate texture brushes (icons)
            if (_baseTabControl == null || _baseTabControl.TabCount == 0 ||
                _baseTabControl.ImageList == null || _drawerItemRects == null ||
                _drawerItemRects.Count == 0)
            {
                return;
            }

            // Calculate lightness and color
            float l = UseColors ? SkinManager.ColorScheme.TextColor.R / 255.0f :
                      SkinManager.Theme == MaterialSkinManager.Themes.LIGHT ? 0f : 1f;

            float r = (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor.R :
                      SkinManager.ColorScheme.PrimaryColor.R) / 255.0f;

            float g = (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor.G :
                      SkinManager.ColorScheme.PrimaryColor.G) / 255.0f;

            float b = (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor.B :
                      SkinManager.ColorScheme.PrimaryColor.B) / 255.0f;

            // Create matrices
            float[][] matrixGray = {
                new float[] {   0,   0,   0,   0,  0}, // Red scale factor
                new float[] {   0,   0,   0,   0,  0}, // Green scale factor
                new float[] {   0,   0,   0,   0,  0}, // Blue scale factor
                new float[] {   0,   0,   0, .7f,  0}, // alpha scale factor
                new float[] {   l,   l,   l,   0,  1}  // offset
            };

            float[][] matrixColor = {
                new float[] {   0,   0,   0,   0,  0}, // Red scale factor
                new float[] {   0,   0,   0,   0,  0}, // Green scale factor
                new float[] {   0,   0,   0,   0,  0}, // Blue scale factor
                new float[] {   0,   0,   0,   1,  0}, // alpha scale factor
                new float[] {   r,   g,   b,   0,  1}  // offset
            };

            ColorMatrix colorMatrixGray = new ColorMatrix(matrixGray);
            ColorMatrix colorMatrixColor = new ColorMatrix(matrixColor);

            using (ImageAttributes grayImageAttributes = new ImageAttributes())
            using (ImageAttributes colorImageAttributes = new ImageAttributes())
            {
                // Set color matrices
                grayImageAttributes.SetColorMatrix(colorMatrixGray, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                colorImageAttributes.SetColorMatrix(colorMatrixColor, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                // Clean up old resources if they exist
                if (_iconsBrushes != null)
                {
                    foreach (var brush in _iconsBrushes.Values)
                    {
                        brush.Dispose();
                    }
                }

                if (_iconsSelectedBrushes != null)
                {
                    foreach (var brush in _iconsSelectedBrushes.Values)
                    {
                        brush.Dispose();
                    }
                }

                // Create brushes
                _iconsBrushes = new Dictionary<string, TextureBrush>(_baseTabControl.TabPages.Count);
                _iconsSelectedBrushes = new Dictionary<string, TextureBrush>(_baseTabControl.TabPages.Count);
                _iconsSize = new Dictionary<string, Rectangle>(_baseTabControl.TabPages.Count);

                foreach (TabPage tabPage in _baseTabControl.TabPages)
                {
                    // Skip items without image
                    if (string.IsNullOrEmpty(tabPage.ImageKey) || _drawerItemRects == null)
                        continue;

                    try
                    {
                        // Image Rect
                        Rectangle destRect = new Rectangle(0, 0,
                            _baseTabControl.ImageList.Images[tabPage.ImageKey].Width,
                            _baseTabControl.ImageList.Images[tabPage.ImageKey].Height);

                        // Create a pre-processed copy of the image (GRAY)
                        using (Bitmap bgray = new Bitmap(destRect.Width, destRect.Height))
                        {
                            using (Graphics gGray = Graphics.FromImage(bgray))
                            {
                                gGray.DrawImage(_baseTabControl.ImageList.Images[tabPage.ImageKey],
                                    new Point[] {
                                        new Point(0, 0),
                                        new Point(destRect.Width, 0),
                                        new Point(0, destRect.Height),
                                    },
                                    destRect, GraphicsUnit.Pixel, grayImageAttributes);
                            }

                            // Create a pre-processed copy of the image (PRIMARY COLOR)
                            using (Bitmap bcolor = new Bitmap(destRect.Width, destRect.Height))
                            {
                                using (Graphics gColor = Graphics.FromImage(bcolor))
                                {
                                    gColor.DrawImage(_baseTabControl.ImageList.Images[tabPage.ImageKey],
                                        new Point[] {
                                            new Point(0, 0),
                                            new Point(destRect.Width, 0),
                                            new Point(0, destRect.Height),
                                        },
                                        destRect, GraphicsUnit.Pixel, colorImageAttributes);
                                }

                                // Get current tab index
                                int currentTabIndex = _baseTabControl.TabPages.IndexOf(tabPage);
                                if (currentTabIndex < 0 || currentTabIndex >= _drawerItemRects.Count)
                                    continue;

                                // Create texture brushes with the processed images
                                TextureBrush textureBrushGray = new TextureBrush(bgray);
                                TextureBrush textureBrushColor = new TextureBrush(bcolor);

                                textureBrushGray.WrapMode = WrapMode.Clamp;
                                textureBrushColor.WrapMode = WrapMode.Clamp;

                                // Calculate icon rectangle
                                Rectangle iconRect = new Rectangle(
                                   _drawerItemRects[currentTabIndex].X + (_drawerItemHeight / 2) - (_baseTabControl.ImageList.Images[tabPage.ImageKey].Width / 2),
                                   _drawerItemRects[currentTabIndex].Y + (_drawerItemHeight / 2) - (_baseTabControl.ImageList.Images[tabPage.ImageKey].Height / 2),
                                   _baseTabControl.ImageList.Images[tabPage.ImageKey].Width,
                                   _baseTabControl.ImageList.Images[tabPage.ImageKey].Height);

                                // Translate brushes to correct positions
                                textureBrushGray.TranslateTransform(
                                    iconRect.X + iconRect.Width / 2 - _baseTabControl.ImageList.Images[tabPage.ImageKey].Width / 2,
                                    iconRect.Y + iconRect.Height / 2 - _baseTabControl.ImageList.Images[tabPage.ImageKey].Height / 2);

                                textureBrushColor.TranslateTransform(
                                    iconRect.X + iconRect.Width / 2 - _baseTabControl.ImageList.Images[tabPage.ImageKey].Width / 2,
                                    iconRect.Y + iconRect.Height / 2 - _baseTabControl.ImageList.Images[tabPage.ImageKey].Height / 2);

                                // Add to dictionaries
                                string ik = string.Concat(tabPage.ImageKey, "_", tabPage.Name);
                                _iconsBrushes[ik] = textureBrushGray;
                                _iconsSelectedBrushes[ik] = textureBrushColor;
                                _iconsSize[ik] = new Rectangle(0, 0, iconRect.Width, iconRect.Height);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing icon for tab {tabPage.Name}: {ex.Message}");
                    }
                }
            }
        }

        public MaterialDrawer()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            Height = 120;
            Width = 250;
            IndicatorWidth = 0;
            _isOpen = false;
            ShowIconsWhenHidden = false;
            AutoHide = false;
            AutoShow = false;
            HighlightWithAccent = true;
            BackgroundWithAccent = false;

            _showHideAnimManager = new AnimationManager
            {
                AnimationType = AnimationType.EaseInOut,
                Increment = 0.04
            };
            _showHideAnimManager.OnAnimationProgress += sender =>
            {
                Invalidate();
                ShowHideAnimation();
            };
            _showHideAnimManager.OnAnimationFinished += sender =>
            {
                if (_baseTabControl != null && _drawerItemRects != null && _drawerItemRects.Count > 0 &&
                    _baseTabControl.SelectedIndex >= 0 && _baseTabControl.SelectedIndex < _drawerItemRects.Count)
                {
                    _rippleSize = _drawerItemRects[_baseTabControl.SelectedIndex].Width;
                }

                if (_isOpen)
                {
                    DrawerEndOpen?.Invoke(this);
                }
                else
                {
                    DrawerEndClose?.Invoke(this);
                }
            };

            SkinManager.ColorSchemeChanged += SkinManager_ColorSchemeChanged;
            SkinManager.ThemeChanged += SkinManager_ThemeChanged;

            _clickAnimManager = new AnimationManager
            {
                AnimationType = AnimationType.EaseOut,
                Increment = 0.04
            };
            _clickAnimManager.OnAnimationProgress += sender => Invalidate();

            MouseWheel += MaterialDrawer_MouseWheel;

            _drawerItemRects = new List<Rectangle>();
            _drawerItemPaths = new List<GraphicsPath>();
        }

        private void SkinManager_ColorSchemeChanged(object sender)
        {
            PreProcessIcons();
        }

        private void SkinManager_ThemeChanged(object sender)
        {
            PreProcessIcons();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from events
                MouseWheel -= MaterialDrawer_MouseWheel;

                if (SkinManager != null)
                {
                    SkinManager.ColorSchemeChanged -= SkinManager_ColorSchemeChanged;
                    SkinManager.ThemeChanged -= SkinManager_ThemeChanged;
                }

                if (_baseTabControl != null)
                {
                    _baseTabControl.Deselected -= BaseTabControl_Deselected;
                    _baseTabControl.SelectedIndexChanged -= BaseTabControl_SelectedIndexChanged;
                    _baseTabControl.ControlAdded -= BaseTabControl_ControlChanged;
                    _baseTabControl.ControlRemoved -= BaseTabControl_ControlChanged;
                }

                // Dispose brushes
                if (_iconsBrushes != null)
                {
                    foreach (var brush in _iconsBrushes.Values)
                    {
                        brush.Dispose();
                    }
                    _iconsBrushes.Clear();
                }

                if (_iconsSelectedBrushes != null)
                {
                    foreach (var brush in _iconsSelectedBrushes.Values)
                    {
                        brush.Dispose();
                    }
                    _iconsSelectedBrushes.Clear();
                }

                // Dispose paths
                if (_drawerItemPaths != null)
                {
                    foreach (var path in _drawerItemPaths)
                    {
                        path.Dispose();
                    }
                    _drawerItemPaths.Clear();
                }
            }

            base.Dispose(disposing);
        }

        private void MaterialDrawer_MouseWheel(object sender, MouseEventArgs e)
        {
            int step = 20;
            if (e.Delta > 0)
            {
                if (Location.Y < 0)
                {
                    Location = new Point(Location.X, Location.Y + step > 0 ? 0 : Location.Y + step);
                    Height = Location.Y + step > 0 ? Parent.Height : Height - step;
                }
            }
            else
            {
                if (_drawerItemRects != null && Height < (8 + _drawerItemHeight) * _drawerItemRects.Count)
                {
                    Location = new Point(Location.X, Location.Y - step);
                    Height += step;
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected override void InitLayout()
        {
            _drawerItemHeight = TAB_HEADER_PADDING * 2 - SkinManager.FORM_PADDING / 2;
            MinWidth = (int)(SkinManager.FORM_PADDING * 1.5 + _drawerItemHeight);
            _showHideAnimManager.SetProgress(_isOpen ? 0 : 1);
            ShowHideAnimation();
            Invalidate();

            base.InitLayout();
        }

        private void ShowHideAnimation()
        {
            var showHideAnimProgress = _showHideAnimManager.GetProgress();
            if (_showHideAnimManager.IsAnimating())
            {
                if (ShowIconsWhenHidden)
                {
                    Location = new Point((int)((-Width + MinWidth) * showHideAnimProgress), Location.Y);
                }
                else
                {
                    Location = new Point((int)(-Width * showHideAnimProgress), Location.Y);
                }
            }
            else
            {
                if (_isOpen)
                {
                    Location = new Point(0, Location.Y);
                }
                else
                {
                    if (ShowIconsWhenHidden)
                    {
                        Location = new Point((int)(-Width + MinWidth), Location.Y);
                    }
                    else
                    {
                        Location = new Point(-Width, Location.Y);
                    }
                }
            }
            UpdateTabRects();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Paint(e);
        }

        private new void Paint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Redraw background
            g.Clear(UseColors ? SkinManager.ColorScheme.PrimaryColor : SkinManager.BackdropColor);

            if (_baseTabControl == null)
                return;

            if (!_clickAnimManager.IsAnimating() || _drawerItemRects == null || _drawerItemRects.Count != _baseTabControl.TabCount)
                UpdateTabRects();

            if (_drawerItemRects == null || _drawerItemRects.Count != _baseTabControl.TabCount || _baseTabControl.SelectedIndex < 0)
                return;

            // Click Animation
            var clickAnimProgress = _clickAnimManager.GetProgress();
            // Show/Hide Drawer Animation
            var showHideAnimProgress = _showHideAnimManager.GetProgress();
            var rSize = (int)(clickAnimProgress * _rippleSize * 1.75);

            int dx = _prevLocation - Location.X;
            _prevLocation = Location.X;

            // Ripple
            if (_clickAnimManager.IsAnimating() && _baseTabControl.SelectedIndex >= 0 && _baseTabControl.SelectedIndex < _drawerItemPaths.Count)
            {
                using (SolidBrush rippleBrush = new SolidBrush(Color.FromArgb((int)(70 - (clickAnimProgress * 70)),
                    UseColors ? SkinManager.ColorScheme.AccentColor : // Using colors
                    SkinManager.Theme == MaterialSkinManager.Themes.LIGHT ? SkinManager.ColorScheme.PrimaryColor : // light theme
                    SkinManager.ColorScheme.LightPrimaryColor))) // dark theme
                {
                    g.SetClip(_drawerItemPaths[_baseTabControl.SelectedIndex]);
                    g.FillEllipse(rippleBrush, new Rectangle(_animationSource.X + dx - (rSize / 2), _animationSource.Y - rSize / 2, rSize, rSize));
                    g.ResetClip();
                }
            }

            // Draw menu items
            foreach (TabPage tabPage in _baseTabControl.TabPages)
            {
                var currentTabIndex = _baseTabControl.TabPages.IndexOf(tabPage);
                if (currentTabIndex < 0 || currentTabIndex >= _drawerItemPaths.Count || currentTabIndex >= _drawerItemRects.Count)
                    continue;

                // Background
                using (Brush bgBrush = new SolidBrush(Color.FromArgb(CalculateAlpha(60, 0, currentTabIndex, clickAnimProgress, 1 - showHideAnimProgress),
                    UseColors ? _backgroundWithAccent ? SkinManager.ColorScheme.AccentColor : SkinManager.ColorScheme.LightPrimaryColor : // using colors
                    _backgroundWithAccent ? SkinManager.ColorScheme.AccentColor : // default accent
                    SkinManager.Theme == MaterialSkinManager.Themes.LIGHT ? SkinManager.ColorScheme.PrimaryColor : // default light
                    SkinManager.ColorScheme.LightPrimaryColor))) // default dark
                {
                    g.FillPath(bgBrush, _drawerItemPaths[currentTabIndex]);
                }

                // Text
                Color textColor = Color.FromArgb(CalculateAlphaZeroWhenClosed(SkinManager.TextHighEmphasisColor.A, UseColors ? SkinManager.TextMediumEmphasisColor.A : 255, currentTabIndex, clickAnimProgress, 1 - showHideAnimProgress), // alpha
                    UseColors ? (currentTabIndex == _baseTabControl.SelectedIndex ? (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor : SkinManager.ColorScheme.PrimaryColor) // Use colors - selected
                    : SkinManager.ColorScheme.TextColor) :  // Use colors - not selected
                    (currentTabIndex == _baseTabControl.SelectedIndex ? (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor : SkinManager.ColorScheme.PrimaryColor) : // selected
                    SkinManager.TextHighEmphasisColor));

                IntPtr textFont = SkinManager.getLogFontByType(MaterialSkinManager.fontType.Subtitle2);

                Rectangle textRect = _drawerItemRects[currentTabIndex];
                textRect.X += _baseTabControl.ImageList != null ? _drawerItemHeight : (int)(SkinManager.FORM_PADDING * 0.75);
                textRect.Width -= SkinManager.FORM_PADDING << 2;

                using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
                {
                    NativeText.DrawTransparentText(tabPage.Text, textFont, textColor, textRect.Location, textRect.Size, NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                }

                // Icons
                if (_baseTabControl.ImageList != null && !string.IsNullOrEmpty(tabPage.ImageKey) &&
                    _iconsBrushes != null && _iconsSelectedBrushes != null && _iconsSize != null)
                {
                    string ik = string.Concat(tabPage.ImageKey, "_", tabPage.Name);

                    if (_iconsBrushes.ContainsKey(ik) && _iconsSelectedBrushes.ContainsKey(ik) && _iconsSize.ContainsKey(ik))
                    {
                        Rectangle iconRect = new Rectangle(
                            _drawerItemRects[currentTabIndex].X + (_drawerItemHeight >> 1) - (_iconsSize[ik].Width >> 1),
                            _drawerItemRects[currentTabIndex].Y + (_drawerItemHeight >> 1) - (_iconsSize[ik].Height >> 1),
                            _iconsSize[ik].Width, _iconsSize[ik].Height);

                        if (ShowIconsWhenHidden)
                        {
                            _iconsBrushes[ik].TranslateTransform(dx, 0);
                            _iconsSelectedBrushes[ik].TranslateTransform(dx, 0);
                        }

                        g.FillRectangle(currentTabIndex == _baseTabControl.SelectedIndex ? _iconsSelectedBrushes[ik] : _iconsBrushes[ik], iconRect);
                    }
                }
            }

            // Draw divider if not using colors
            if (!UseColors)
            {
                using (Pen dividerPen = new Pen(SkinManager.DividersColor, 1))
                {
                    g.DrawLine(dividerPen, Width - 1, 0, Width - 1, Height);
                }
            }

            // Animate tab indicator
            if (_baseTabControl.SelectedIndex >= 0 && _drawerItemRects.Count > 0)
            {
                var previousSelectedTabIndexIfHasOne = _previousSelectedTabIndex == -1 || _previousSelectedTabIndex >= _drawerItemRects.Count ?
                                                       _baseTabControl.SelectedIndex : _previousSelectedTabIndex;

                if (previousSelectedTabIndexIfHasOne >= 0 && previousSelectedTabIndexIfHasOne < _drawerItemRects.Count &&
                    _baseTabControl.SelectedIndex < _drawerItemRects.Count)
                {
                    var previousActiveTabRect = _drawerItemRects[previousSelectedTabIndexIfHasOne];
                    var activeTabPageRect = _drawerItemRects[_baseTabControl.SelectedIndex];

                    var y = previousActiveTabRect.Y + (int)((activeTabPageRect.Y - previousActiveTabRect.Y) * clickAnimProgress);
                    var x = ShowIconsWhenHidden ? -Location.X : 0;
                    var height = _drawerItemHeight;

                    if (IndicatorWidth > 0)
                    {
                        using (SolidBrush accentBrush = new SolidBrush(SkinManager.ColorScheme.AccentColor))
                        {
                            g.FillRectangle(accentBrush, x, y, IndicatorWidth, height);
                        }
                    }
                }
            }
        }

        public new void Show()
        {
            _isOpen = true;
            DrawerStateChanged?.Invoke(this);
            DrawerBeginOpen?.Invoke(this);
            _showHideAnimManager.StartNewAnimation(AnimationDirection.Out);
        }

        public new void Hide()
        {
            _isOpen = false;
            DrawerStateChanged?.Invoke(this);
            DrawerBeginClose?.Invoke(this);
            _showHideAnimManager.StartNewAnimation(AnimationDirection.In);
        }

        public void Toggle()
        {
            if (_isOpen)
                Hide();
            else
                Show();
        }

        private int CalculateAlphaZeroWhenClosed(int primaryA, int secondaryA, int tabIndex, double clickAnimProgress, double showHideAnimProgress)
        {
            // Drawer is closed
            if (!_isOpen && !_showHideAnimManager.IsAnimating())
            {
                return 0;
            }
            // Active menu (no change)
            if (tabIndex == _baseTabControl.SelectedIndex && (!_clickAnimManager.IsAnimating() || _showHideAnimManager.IsAnimating()))
            {
                return (int)(primaryA * showHideAnimProgress);
            }
            // Previous menu (changing)
            if (tabIndex == _previousSelectedTabIndex && !_showHideAnimManager.IsAnimating())
            {
                return primaryA - (int)((primaryA - secondaryA) * clickAnimProgress);
            }
            // Inactive menu (no change)
            if (tabIndex != _baseTabControl.SelectedIndex)
            {
                return (int)(secondaryA * showHideAnimProgress);
            }
            // Active menu (changing)
            return secondaryA + (int)((primaryA - secondaryA) * clickAnimProgress);
        }

        private int CalculateAlpha(int primaryA, int secondaryA, int tabIndex, double clickAnimProgress, double showHideAnimProgress)
        {
            if (tabIndex == _baseTabControl.SelectedIndex && !_clickAnimManager.IsAnimating())
            {
                return (int)(primaryA);
            }
            if (tabIndex != _previousSelectedTabIndex && tabIndex != _baseTabControl.SelectedIndex)
            {
                return secondaryA;
            }
            if (tabIndex == _previousSelectedTabIndex)
            {
                return primaryA - (int)((primaryA - secondaryA) * clickAnimProgress);
            }
            return secondaryA + (int)((primaryA - secondaryA) * clickAnimProgress);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (_drawerItemRects == null)
                UpdateTabRects();
            for (var i = 0; i < _drawerItemRects.Count; i++)
            {
                if (_drawerItemRects[i].Contains(e.Location) && _lastLocationY == Location.Y)
                {
                    _baseTabControl.SelectedIndex = i;
                    if (AutoHide && !AutoShow)
                        Hide();
                }
            }

            _animationSource = e.Location;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _lastMouseY = e.Y;
            _lastLocationY = Location.Y; // memorize Y location of drawer
            base.OnMouseDown(e);
            if (DesignMode)
                return;
            MouseState = MouseState.DOWN;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (DesignMode)
                return;
            MouseState = MouseState.OUT;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (DesignMode)
                return;

            if (e.Button == MouseButtons.Left && e.Y != _lastMouseY && (Location.Y < 0 || Height < (8 + _drawerItemHeight) * _drawerItemRects.Count))
            {
                int diff = e.Y - _lastMouseY;
                if (diff > 0)
                {
                    if (Location.Y < 0)
                    {
                        Location = new Point(Location.X, Location.Y + diff > 0 ? 0 : Location.Y + diff);
                        Height = Parent.Height + Math.Abs(Location.Y);
                    }
                }
                else
                {
                    if (Height < (8 + _drawerItemHeight) * _drawerItemRects.Count)
                    {
                        Location = new Point(Location.X, Location.Y + diff);
                        Height = Parent.Height + Math.Abs(Location.Y);
                    }
                }
                //return;
            }

            base.OnMouseMove(e);

            if (_drawerItemRects == null)
                UpdateTabRects();

            Cursor previousCursor = Cursor;

            if (e.Location.X + this.Location.X < BORDER_WIDTH)
            {
                if (e.Location.Y > this.Height - BORDER_WIDTH)
                    Cursor = Cursors.SizeNESW;                  //Bottom Left
                else
                    Cursor = Cursors.SizeWE;                    //Left
            }
            else if (e.Location.Y > this.Height - BORDER_WIDTH)
            {
                Cursor = Cursors.SizeNS;                        //Bottom
            }
            else
            {
                if (e.Location.Y < _drawerItemRects[_drawerItemRects.Count - 1].Bottom && (e.Location.X + this.Location.X) >= BORDER_WIDTH)
                    Cursor = Cursors.Hand;
                else
                    Cursor = Cursors.Default;
            }

            if (previousCursor != Cursor) CursorUpdate?.Invoke(this, Cursor);

        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (AutoShow && _isOpen == false)
            {
                Show();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (MouseState != MouseState.DOWN)
            {
                Cursor = Cursors.Default;
                CursorUpdate?.Invoke(this, Cursor);
            }

            if (AutoShow)
            {
                Hide();
            }
        }

        private void UpdateTabRects()
        {
            //If there isn't a base tab control, the rects shouldn't be calculated
            //or if there aren't tab pages in the base tab control, the list should just be empty
            if (_baseTabControl == null || _baseTabControl.TabCount == 0 || SkinManager == null || _drawerItemRects == null)
            {
                _drawerItemRects = new List<Rectangle>();
                _drawerItemPaths = new List<GraphicsPath>();
                return;
            }

            if (_drawerItemRects.Count != _baseTabControl.TabCount)
            {
                _drawerItemRects = new List<Rectangle>(_baseTabControl.TabCount);
                _drawerItemPaths = new List<GraphicsPath>(_baseTabControl.TabCount);

                for (var i = 0; i < _baseTabControl.TabCount; i++)
                {
                    _drawerItemRects.Add(new Rectangle());
                    _drawerItemPaths.Add(new GraphicsPath());
                }
            }

            //Calculate the bounds of each tab header specified in the base tab control
            for (int i = 0; i < _baseTabControl.TabPages.Count; i++)
            {
                _drawerItemRects[i] = (new Rectangle(
                    (int)(SkinManager.FORM_PADDING * 0.75) - (ShowIconsWhenHidden ? Location.X : 0),
                    (TAB_HEADER_PADDING * 2) * i + (int)(SkinManager.FORM_PADDING >> 1),
                    (Width + (ShowIconsWhenHidden ? Location.X : 0)) - (int)(SkinManager.FORM_PADDING * 1.5) - 1,
                    _drawerItemHeight));

                _drawerItemPaths[i] = DrawHelper.CreateRoundRect(new RectangleF(_drawerItemRects[i].X - 0.5f, _drawerItemRects[i].Y - 0.5f, _drawerItemRects[i].Width, _drawerItemRects[i].Height), 4);
            }
        }
    }
}
