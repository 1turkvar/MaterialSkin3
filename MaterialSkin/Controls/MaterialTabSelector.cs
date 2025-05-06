namespace MaterialSkin.Controls
{
    using MaterialSkin.Animations;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Text;
    using System.Globalization;
    using System.Windows.Forms;

    public class MaterialTabSelector : Control, IMaterialControl
    {
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        public enum CustomCharacterCasing
        {
            [Description("Text will be used as user inserted, no alteration")]
            Normal,
            [Description("Text will be converted to UPPER case")]
            Upper,
            [Description("Text will be converted to lower case")]
            Lower,
            [Description("Text will be converted to Proper case (aka Title case)")]
            Proper
        }

        private readonly TextInfo _textInfo = new CultureInfo("en-US", false).TextInfo;

        private MaterialTabControl _baseTabControl;

        [Category("Material Skin"), Browsable(true)]
        public MaterialTabControl BaseTabControl
        {
            get { return _baseTabControl; }
            set
            {
                _baseTabControl = value;
                if (_baseTabControl == null) return;

                UpdateTabRects();

                _previousSelectedTabIndex = _baseTabControl.SelectedIndex;
                _baseTabControl.Deselected += (sender, args) =>
                {
                    _previousSelectedTabIndex = _baseTabControl.SelectedIndex;
                };
                _baseTabControl.SelectedIndexChanged += (sender, args) =>
                {
                    _animationManager.SetProgress(0);
                    _animationManager.StartNewAnimation(AnimationDirection.In);
                };
                _baseTabControl.ControlAdded += delegate
                {
                    Invalidate();
                };
                _baseTabControl.ControlRemoved += delegate
                {
                    Invalidate();
                };
            }
        }

        private int _previousSelectedTabIndex;

        private Point _animationSource;

        private readonly AnimationManager _animationManager;

        private List<Rectangle> _tabRects;

        private const int ICON_SIZE = 24;
        private const int FIRST_TAB_PADDING = 50;
        private const int TAB_HEADER_PADDING = 24;
        private const int TAB_WIDTH_MIN = 160;
        private const int TAB_WIDTH_MAX = 264;

        private int _tabOverIndex = -1;

        private CustomCharacterCasing _characterCasing;

        [Category("Appearance")]
        public CustomCharacterCasing CharacterCasing
        {
            get { return _characterCasing; }
            set
            {
                _characterCasing = value;
                if (_baseTabControl != null)
                {
                    _baseTabControl.Invalidate();
                }
                Invalidate();
            }
        }

        private int _tabIndicatorHeight;

        [Category("Material Skin"), Browsable(true), DisplayName("Tab Indicator Height"), DefaultValue(2)]
        public int TabIndicatorHeight
        {
            get { return _tabIndicatorHeight; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(TabIndicatorHeight), value, "Value should be > 0");
                else
                {
                    _tabIndicatorHeight = value;
                    Refresh();
                }
            }
        }

        public enum TabLabelStyle
        {
            Text,
            Icon,
            IconAndText,
        }

        private TabLabelStyle _tabLabel;

        [Category("Material Skin"), Browsable(true), DisplayName("Tab Label"), DefaultValue(TabLabelStyle.Text)]
        public TabLabelStyle TabLabel
        {
            get { return _tabLabel; }
            set
            {
                _tabLabel = value;
                Height = (_tabLabel == TabLabelStyle.IconAndText) ? 72 : 48;
                UpdateTabRects();
                Invalidate();
            }
        }

        public MaterialTabSelector()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);
            _tabIndicatorHeight = 2;
            _tabLabel = TabLabelStyle.Text;

            Size = new Size(480, 48);

            _animationManager = new AnimationManager
            {
                AnimationType = AnimationType.EaseOut,
                Increment = 0.04
            };
            _animationManager.OnAnimationProgress += sender => Invalidate();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            Font = SkinManager.getFontByType(MaterialSkinManager.fontType.Body1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            g.Clear(SkinManager.ColorScheme.PrimaryColor);

            if (_baseTabControl == null) return;

            if (!_animationManager.IsAnimating() || _tabRects == null || _tabRects.Count != _baseTabControl.TabCount)
                UpdateTabRects();

            var animationProgress = _animationManager.GetProgress();

            // Click feedback
            if (_animationManager.IsAnimating())
            {
                using (var rippleBrush = new SolidBrush(Color.FromArgb((int)(51 - (animationProgress * 50)), Color.White)))
                {
                    var rippleSize = (int)(animationProgress * _tabRects[_baseTabControl.SelectedIndex].Width * 1.75);

                    g.SetClip(_tabRects[_baseTabControl.SelectedIndex]);
                    g.FillEllipse(rippleBrush, new Rectangle(_animationSource.X - rippleSize / 2, _animationSource.Y - rippleSize / 2, rippleSize, rippleSize));
                    g.ResetClip();
                }
            }

            // Draw tab headers
            if (_tabOverIndex >= 0 && _tabOverIndex < _tabRects.Count)
            {
                // Change mouse over tab background color
                g.FillRectangle(SkinManager.BackgroundHoverBrush, _tabRects[_tabOverIndex].X, _tabRects[_tabOverIndex].Y, _tabRects[_tabOverIndex].Width, _tabRects[_tabOverIndex].Height - _tabIndicatorHeight);
            }

            foreach (TabPage tabPage in _baseTabControl.TabPages)
            {
                var currentTabIndex = _baseTabControl.TabPages.IndexOf(tabPage);
                if (currentTabIndex >= _tabRects.Count) continue;

                if (_tabLabel != TabLabelStyle.Icon)
                {
                    // Text
                    using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                    {
                        Size textSize = TextRenderer.MeasureText(tabPage.Text, Font);
                        Rectangle textLocation = new Rectangle(
                            _tabRects[currentTabIndex].X + (TAB_HEADER_PADDING / 2),
                            _tabRects[currentTabIndex].Y,
                            _tabRects[currentTabIndex].Width - TAB_HEADER_PADDING,
                            _tabRects[currentTabIndex].Height);

                        if (_tabLabel == TabLabelStyle.IconAndText)
                        {
                            textLocation.Y = 46;
                            textLocation.Height = 10;
                        }

                        string processedText = ProcessText(tabPage.Text);

                        if ((TAB_HEADER_PADDING * 2) + textSize.Width < TAB_WIDTH_MAX)
                        {
                            nativeText.DrawTransparentText(
                                processedText,
                                Font,
                                Color.FromArgb(CalculateTextAlpha(currentTabIndex, animationProgress), SkinManager.ColorScheme.TextColor),
                                textLocation.Location,
                                textLocation.Size,
                                NativeTextRenderer.TextAlignFlags.Center | NativeTextRenderer.TextAlignFlags.Middle);
                        }
                        else
                        {
                            if (_tabLabel == TabLabelStyle.IconAndText)
                            {
                                textLocation.Y = 40;
                                textLocation.Height = 26;
                            }
                            nativeText.DrawMultilineTransparentText(
                                processedText,
                                SkinManager.getFontByType(MaterialSkinManager.fontType.Body2),
                                Color.FromArgb(CalculateTextAlpha(currentTabIndex, animationProgress), SkinManager.ColorScheme.TextColor),
                                textLocation.Location,
                                textLocation.Size,
                                NativeTextRenderer.TextAlignFlags.Center | NativeTextRenderer.TextAlignFlags.Middle);
                        }
                    }
                }

                if (_tabLabel != TabLabelStyle.Text)
                {
                    // Icons
                    if (_baseTabControl.ImageList != null && (!string.IsNullOrEmpty(tabPage.ImageKey) || tabPage.ImageIndex > -1))
                    {
                        Rectangle iconRect = new Rectangle(
                            _tabRects[currentTabIndex].X + (_tabRects[currentTabIndex].Width / 2) - (ICON_SIZE / 2),
                            _tabRects[currentTabIndex].Y + (_tabRects[currentTabIndex].Height / 2) - (ICON_SIZE / 2),
                            ICON_SIZE, ICON_SIZE);

                        if (_tabLabel == TabLabelStyle.IconAndText)
                        {
                            iconRect.Y = 12;
                        }

                        Image imageToDisplay = !string.IsNullOrEmpty(tabPage.ImageKey)
                            ? _baseTabControl.ImageList.Images[tabPage.ImageKey]
                            : _baseTabControl.ImageList.Images[tabPage.ImageIndex];

                        g.DrawImage(imageToDisplay, iconRect);
                    }
                }
            }

            // Animate tab indicator
            if (_baseTabControl.TabCount > 0)
            {
                int safeSelectedIndex = Math.Min(_baseTabControl.SelectedIndex, _tabRects.Count - 1);
                int safePreviousIndex = _previousSelectedTabIndex == -1
                    ? safeSelectedIndex
                    : Math.Min(_previousSelectedTabIndex, _tabRects.Count - 1);

                var previousActiveTabRect = _tabRects[safePreviousIndex];
                var activeTabPageRect = _tabRects[safeSelectedIndex];

                var y = activeTabPageRect.Bottom - _tabIndicatorHeight;
                var x = previousActiveTabRect.X + (int)((activeTabPageRect.X - previousActiveTabRect.X) * animationProgress);
                var width = previousActiveTabRect.Width + (int)((activeTabPageRect.Width - previousActiveTabRect.Width) * animationProgress);

                g.FillRectangle(SkinManager.ColorScheme.AccentBrush, x, y, width, _tabIndicatorHeight);
            }
        }

        private string ProcessText(string text)
        {
            switch (_characterCasing)
            {
                case CustomCharacterCasing.Upper:
                    return text.ToUpper();
                case CustomCharacterCasing.Lower:
                    return text.ToLower();
                case CustomCharacterCasing.Proper:
                    return _textInfo.ToTitleCase(text.ToLower());
                default:
                    return text;
            }
        }

        private int CalculateTextAlpha(int tabIndex, double animationProgress)
        {
            int primaryA = SkinManager.TextHighEmphasisColor.A;
            int secondaryA = SkinManager.TextMediumEmphasisColor.A;

            if (tabIndex == _baseTabControl.SelectedIndex && !_animationManager.IsAnimating())
            {
                return primaryA;
            }
            if (tabIndex != _previousSelectedTabIndex && tabIndex != _baseTabControl.SelectedIndex)
            {
                return secondaryA;
            }
            if (tabIndex == _previousSelectedTabIndex)
            {
                return primaryA - (int)((primaryA - secondaryA) * animationProgress);
            }
            return secondaryA + (int)((primaryA - secondaryA) * animationProgress);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_baseTabControl == null) return;

            if (_tabRects == null) UpdateTabRects();

            for (var i = 0; i < _tabRects.Count; i++)
            {
                if (_tabRects[i].Contains(e.Location))
                {
                    _baseTabControl.SelectedIndex = i;
                    break;
                }
            }

            _animationSource = e.Location;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (DesignMode)
                return;

            if (_tabRects == null)
                UpdateTabRects();

            int oldTabOverIndex = _tabOverIndex;
            _tabOverIndex = -1;

            for (var i = 0; i < _tabRects.Count; i++)
            {
                if (_tabRects[i].Contains(e.Location))
                {
                    Cursor = Cursors.Hand;
                    _tabOverIndex = i;
                    break;
                }
            }

            if (_tabOverIndex == -1)
                Cursor = Cursors.Arrow;

            if (oldTabOverIndex != _tabOverIndex)
                Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (DesignMode)
                return;

            Cursor = Cursors.Arrow;
            _tabOverIndex = -1;
            Invalidate();
        }

        private void UpdateTabRects()
        {
            _tabRects = new List<Rectangle>();

            // If there isn't a base tab control, the rects shouldn't be calculated
            // If there aren't tab pages in the base tab control, the list should just be empty which has been set already; exit the void
            if (_baseTabControl == null || _baseTabControl.TabCount == 0) return;

            // Calculate the bounds of each tab header specified in the base tab control
            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                for (int i = 0; i < _baseTabControl.TabPages.Count; i++)
                {
                    Size textSize = TextRenderer.MeasureText(_baseTabControl.TabPages[i].Text, Font);
                    if (_tabLabel == TabLabelStyle.Icon) textSize.Width = ICON_SIZE;

                    int tabWidth = (TAB_HEADER_PADDING * 2) + textSize.Width;
                    if (tabWidth > TAB_WIDTH_MAX)
                        tabWidth = TAB_WIDTH_MAX;
                    else if (tabWidth < TAB_WIDTH_MIN)
                        tabWidth = TAB_WIDTH_MIN;

                    if (i == 0)
                        _tabRects.Add(new Rectangle(FIRST_TAB_PADDING - TAB_HEADER_PADDING, 0, tabWidth, Height));
                    else
                        _tabRects.Add(new Rectangle(_tabRects[i - 1].Right, 0, tabWidth, Height));
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}