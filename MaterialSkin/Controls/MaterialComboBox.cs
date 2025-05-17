namespace MaterialSkin.Controls
{
    using MaterialSkin.Animations;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using System.Data;
    using System.Windows.Forms;
    using System.Drawing.Drawing2D;

    public class MaterialComboBox : ComboBox, IMaterialControl
    {
        // For some reason, even when overriding the AutoSize property, it doesn't appear on the properties panel, so we have to create a new one.
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Category("Layout")]
        private bool _AutoResize;

        public bool AutoResize
        {
            get { return _AutoResize; }
            set
            {
                _AutoResize = value;
                RecalculateAutoSize();
            }
        }

        //Properties for managing the material design properties
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        private bool _UseTallSize;

        [Category("Material Skin"), DefaultValue(true), Description("Using a larger size enables the hint to always be visible")]
        public bool UseTallSize
        {
            get { return _UseTallSize; }
            set
            {
                _UseTallSize = value;
                SetHeightVars();
                Invalidate();
            }
        }

        [Category("Material Skin"), DefaultValue(true)]
        public bool UseAccent { get; set; } = true;

        private string _hint = string.Empty;

        [Category("Material Skin"), DefaultValue(""), Localizable(true)]
        public string Hint
        {
            get { return _hint; }
            set
            {
                _hint = value;
                hasHint = !String.IsNullOrEmpty(Hint);
                Invalidate();
            }
        }

        private int _startIndex;
        [Category("Material Skin"), DefaultValue(0)]
        public int StartIndex
        {
            get => _startIndex;
            set
            {
                _startIndex = value;
                try
                {
                    if (value >= 0 && value < Items.Count)
                    {
                        SelectedIndex = value;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error setting StartIndex", ex);
                }
                Invalidate();
            }
        }

        private const int TEXT_SMALL_SIZE = 18;
        private const int TEXT_SMALL_Y = 4;
        private const int BOTTOM_PADDING = 3;
        private int HEIGHT = 50;
        private int LINE_Y;

        private bool hasHint;
        private string _displayedText = string.Empty;
        private bool _useCustomText = false;
        private bool _allowCustomText = false; // Yeni property için backing field

        private readonly AnimationManager _animationManager;

        public MaterialComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            // Material Properties
            Hint = "";
            UseTallSize = true;
            MaxDropDownItems = 4;
            _UseTallSize = true; // Initialize backing field
                                 // Default değerler
            _allowCustomText = false; // Default olarak kapalı

            Font = SkinManager.getFontByType(MaterialSkinManager.fontType.Subtitle2);
            BackColor = SkinManager.BackgroundColor;
            ForeColor = SkinManager.TextHighEmphasisColor;
            DrawMode = DrawMode.OwnerDrawVariable;
            DropDownStyle = ComboBoxStyle.DropDownList;
            DropDownWidth = Width;

            // Animations
            _animationManager = new AnimationManager(true)
            {
                Increment = 0.08,
                AnimationType = AnimationType.EaseInOut
            };
            _animationManager.OnAnimationProgress += sender => Invalidate();
            _animationManager.OnAnimationFinished += sender => _animationManager.SetProgress(0);

            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            DropDownClosed += (sender, args) =>
            {
                MouseState = MouseState.OUT;
                if (SelectedIndex < 0 && !Focused) _animationManager.StartNewAnimation(AnimationDirection.Out);
            };

            LostFocus += (sender, args) =>
            {
                MouseState = MouseState.OUT;
                if (SelectedIndex < 0) _animationManager.StartNewAnimation(AnimationDirection.Out);
            };

            DropDown += (sender, args) =>
            {
                _animationManager.StartNewAnimation(AnimationDirection.In);
            };

            GotFocus += (sender, args) =>
            {
                _animationManager.StartNewAnimation(AnimationDirection.In);
                Invalidate();
            };

            MouseEnter += (sender, args) =>
            {
                MouseState = MouseState.HOVER;
                Invalidate();
            };

            MouseLeave += (sender, args) =>
            {
                MouseState = MouseState.OUT;
                Invalidate();
            };

            SelectedIndexChanged += (sender, args) =>
            {
                // Update displayed text when selection changes
                UpdateDisplayedText();
                Invalidate();
            };

            KeyUp += (sender, args) =>
            {
                if (Enabled && DropDownStyle == ComboBoxStyle.DropDownList && (args.KeyCode == Keys.Delete || args.KeyCode == Keys.Back))
                {
                    SelectedIndex = -1;
                    UpdateDisplayedText();
                    Invalidate();
                }
            };
        }

        private void UpdateDisplayedText()
        {
            if (!_allowCustomText || !_useCustomText)
            {
                if (SelectedIndex >= 0)
                {
                    _displayedText = GetItemText(SelectedIndex);
                }
                else
                {
                    _displayedText = string.Empty;
                }
                _useCustomText = false;
            }
            // _allowCustomText true ve _useCustomText true ise _displayedText'i değiştirme
        }

        [Category("Misc"), DefaultValue(false)]
        [Description("Items listesinde olmayan değerleri Text property'sine yazabilmeyi sağlar")]
        public bool AllowCustomText
        {
            get { return _allowCustomText; }
            set
            {
                _allowCustomText = value;

                // Eğer özellik devre dışı bırakılıyorsa custom text'i temizle
                if (!value && _useCustomText)
                {
                    _useCustomText = false;
                    // Mevcut SelectedIndex'e göre güncelle
                    if (SelectedIndex >= 0)
                    {
                        _displayedText = GetItemText(SelectedIndex);
                    }
                    else
                    {
                        _displayedText = string.Empty;
                    }
                    Invalidate();
                }
            }
        }

        // Text property - AllowCustomText'e göre davranır
        [Browsable(false)]
        public override string Text
        {
            get
            {
                // Eğer custom text izinli ve aktifse onu döndür
                if (_allowCustomText && _useCustomText)
                {
                    return _displayedText;
                }

                // Aksi halde normal davranışı sürdür
                if (SelectedIndex >= 0)
                {
                    return GetItemText(SelectedIndex);
                }
                return _displayedText;
            }
            set
            {
                // AllowCustomText false ise sadece Items içinde arama yap
                if (!_allowCustomText)
                {
                    // Items içinde arama yap
                    if (!string.IsNullOrEmpty(value) && Items.Count > 0)
                    {
                        for (int i = 0; i < Items.Count; i++)
                        {
                            if (string.Equals(GetItemText(i), value, StringComparison.OrdinalIgnoreCase))
                            {
                                SelectedIndex = i;
                                return;
                            }
                        }
                    }

                    // Eşleşme bulunamadıysa veya value boşsa SelectedIndex temizle
                    SelectedIndex = -1;
                    _displayedText = string.Empty;
                    _useCustomText = false;
                    Invalidate();
                    return;
                }

                // AllowCustomText true ise önceki davranışı sürdür
                _displayedText = value ?? string.Empty;
                _useCustomText = true;

                // Items içinde arama yap (opsiyonel)
                if (!string.IsNullOrEmpty(value) && Items.Count > 0)
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (string.Equals(GetItemText(i), value, StringComparison.OrdinalIgnoreCase))
                        {
                            SelectedIndex = i;
                            _useCustomText = false;
                            return;
                        }
                    }
                }

                // Eşleşme bulunamadı, SelectedIndex'i temizle
                SelectedIndex = -1;
                Invalidate();
            }
        }


        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            if (SelectedIndex >= 0)
            {
                _useCustomText = false; // Programatik seçim yapıldı, custom text modunu devre dışı bırak
                _displayedText = GetItemText(SelectedIndex);
            }
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        // Override Items collection to force update when items are added
        protected override void RefreshItems()
        {
            base.RefreshItems();
            // StartIndex'i uygula
            if (_startIndex >= 0 && _startIndex < Items.Count && SelectedIndex < 0)
            {
                SelectedIndex = _startIndex;
            }
            UpdateDisplayedText();
            Invalidate();
        }

        protected override void OnDataSourceChanged(EventArgs e)
        {
            base.OnDataSourceChanged(e);
            // StartIndex'i uygula
            if (_startIndex >= 0 && _startIndex < Items.Count && SelectedIndex < 0)
            {
                SelectedIndex = _startIndex;
            }
            UpdateDisplayedText();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.Clear(Parent.BackColor);

            // Create rounded rectangle for the background
            using (GraphicsPath roundedRectPath = DrawHelper.CreateRoundRect(
                ClientRectangle.X,
                ClientRectangle.Y,
                ClientRectangle.Width,
                LINE_Y,
                4))
            {
                // Determine background color based on state
                using (SolidBrush fillBrush = GetBackgroundBrush())
                {
                    g.FillPath(fillBrush, roundedRectPath);
                }
            }

            //Set color and brush
            Color selectedColor = UseAccent ? SkinManager.ColorScheme.AccentColor : SkinManager.ColorScheme.PrimaryColor;
            using (SolidBrush selectedBrush = new SolidBrush(selectedColor))
            {
                DrawArrow(g, selectedBrush);
                DrawHintAndText(g, selectedColor, selectedBrush);
            }
        }

        private SolidBrush GetBackgroundBrush()
        {
            if (!Enabled)
                return new SolidBrush(SkinManager.BackgroundDisabledColor);

            if (Focused)
                return new SolidBrush(SkinManager.BackgroundFocusColor);

            if (MouseState == MouseState.HOVER)
                return new SolidBrush(SkinManager.BackgroundHoverColor);

            return new SolidBrush(SkinManager.BackgroundAlternativeColor);
        }

        private void DrawArrow(Graphics g, SolidBrush selectedBrush)
        {
            // Create and Draw the arrow
            using (GraphicsPath pth = new GraphicsPath())
            {
                float centerY = Height * 0.5f;
                PointF topRight = new PointF(Width - 0.5f - SkinManager.FORM_PADDING, centerY - 2.5f);
                PointF midBottom = new PointF(Width - 4.5f - SkinManager.FORM_PADDING, centerY + 2.5f);
                PointF topLeft = new PointF(Width - 8.5f - SkinManager.FORM_PADDING, centerY - 2.5f);

                pth.AddLine(topLeft, topRight);
                pth.AddLine(topRight, midBottom);

                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (Enabled)
                {
                    g.FillPath(DroppedDown || Focused ? selectedBrush : SkinManager.TextHighEmphasisBrush, pth);
                }
                else
                {
                    using (SolidBrush disabledBrush = new SolidBrush(DrawHelper.BlendColor(SkinManager.TextHighEmphasisColor, SkinManager.SwitchOffDisabledThumbColor, 197)))
                    {
                        g.FillPath(disabledBrush, pth);
                    }
                }

                g.SmoothingMode = SmoothingMode.None;
            }
        }

        // DrawHintAndText method'unu güncelle
        private void DrawHintAndText(Graphics g, Color selectedColor, SolidBrush selectedBrush)
        {
            bool userTextPresent = (_allowCustomText && _useCustomText) || SelectedIndex >= 0 || !string.IsNullOrEmpty(_displayedText);

            // ... mevcut hint ve line drawing kodu ...
            Rectangle hintRect = new Rectangle(SkinManager.FORM_PADDING, ClientRectangle.Y, Width, LINE_Y);
            int hintTextSize = 16;

            // bottom line base
            g.FillRectangle(SkinManager.DividersAlternativeBrush, 0, LINE_Y, Width, 1);

            if (!_animationManager.IsAnimating())
            {
                // No animation
                if (hasHint && UseTallSize && (DroppedDown || Focused || userTextPresent))
                {
                    // hint text
                    hintRect = new Rectangle(SkinManager.FORM_PADDING, TEXT_SMALL_Y, Width, TEXT_SMALL_SIZE);
                    hintTextSize = 12;
                }

                // bottom line
                if (DroppedDown || Focused)
                {
                    g.FillRectangle(selectedBrush, 0, LINE_Y, Width, 2);
                }
            }
            else
            {
                // Animation logic...
                double animationProgress = _animationManager.GetProgress();

                // hint Animation
                if (hasHint && UseTallSize)
                {
                    int hintY = userTextPresent && !_animationManager.IsAnimating() ?
                        TEXT_SMALL_Y :
                        ClientRectangle.Y + (int)((TEXT_SMALL_Y - ClientRectangle.Y) * animationProgress);

                    int hintHeight = userTextPresent && !_animationManager.IsAnimating() ?
                        TEXT_SMALL_SIZE :
                        (int)(LINE_Y + (TEXT_SMALL_SIZE - LINE_Y) * animationProgress);

                    hintRect = new Rectangle(SkinManager.FORM_PADDING, hintY, Width, hintHeight);
                    hintTextSize = userTextPresent && !_animationManager.IsAnimating() ? 12 : (int)(16 + (12 - 16) * animationProgress);
                }

                // Line Animation
                int lineAnimationWidth = (int)(Width * animationProgress);
                int lineAnimationX = (Width / 2) - (lineAnimationWidth / 2);
                g.FillRectangle(selectedBrush, lineAnimationX, LINE_Y, lineAnimationWidth, 2);
            }

            // Calc text Rect
            Rectangle textRect = new Rectangle(
                SkinManager.FORM_PADDING,
                hasHint && UseTallSize ? (hintRect.Y + hintRect.Height) - 2 : ClientRectangle.Y,
                ClientRectangle.Width - SkinManager.FORM_PADDING * 3 - 8,
                hasHint && UseTallSize ? LINE_Y - (hintRect.Y + hintRect.Height) : LINE_Y);

            // Draw user text
            if (userTextPresent)
            {
                using (Region clipRegion = new Region(textRect))
                {
                    g.Clip = clipRegion;

                    using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                    {
                        // Text property'sini kullan
                        string displayText = this.Text;

                        nativeText.DrawTransparentText(
                            displayText,
                            SkinManager.getLogFontByType(MaterialSkinManager.fontType.Subtitle1),
                            Enabled ? SkinManager.TextHighEmphasisColor : SkinManager.TextDisabledOrHintColor,
                            textRect.Location,
                            textRect.Size,
                            NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                    }

                    g.ResetClip();
                }
            }

            // Draw hint text
            if (hasHint && (UseTallSize || !userTextPresent))
            {
                using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                {
                    Color hintColor = !Enabled ? SkinManager.TextDisabledOrHintColor :
                        (DroppedDown || Focused) ? selectedColor : SkinManager.TextMediumEmphasisColor;

                    nativeText.DrawTransparentText(
                        Hint,
                        SkinManager.getTextBoxFontBySize(hintTextSize),
                        hintColor,
                        hintRect.Location,
                        hintRect.Size,
                        NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                }
            }
        }
        // Helper methods (opsiyonel)
        public void SetCustomText(string text)
        {
            if (!_allowCustomText)
            {
                throw new InvalidOperationException("AllowCustomText property must be true to use SetCustomText method.");
            }

            _displayedText = text ?? string.Empty;
            _useCustomText = true;
            SelectedIndex = -1;
            Invalidate();
        }

        public void ClearCustomText()
        {
            _useCustomText = false;
            _displayedText = string.Empty;
            SelectedIndex = -1;
            Invalidate();
        }

        private void CustomMeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = HEIGHT - 7;
        }

        private void CustomDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count || !Focused) return;

            Graphics g = e.Graphics;

            // Draw the background of the item.
            g.FillRectangle(SkinManager.BackgroundBrush, e.Bounds);

            // Hover
            if (e.State.HasFlag(DrawItemState.Focus)) // Focus == hover
            {
                g.FillRectangle(SkinManager.BackgroundHoverBrush, e.Bounds);
            }

            string itemText = GetItemText(e.Index);

            using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
            {
                nativeText.DrawTransparentText(
                    itemText,
                    SkinManager.getFontByType(MaterialSkinManager.fontType.Subtitle1),
                    SkinManager.TextHighEmphasisNoAlphaColor,
                    new Point(e.Bounds.Location.X + SkinManager.FORM_PADDING, e.Bounds.Location.Y),
                    new Size(e.Bounds.Size.Width - SkinManager.FORM_PADDING * 2, e.Bounds.Size.Height),
                    NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
            }
        }

        private string GetItemText(int index)
        {
            if (index < 0 || index >= Items.Count)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(DisplayMember))
            {
                var item = Items[index];
                if (item is DataRowView dataRowView)
                {
                    return dataRowView[DisplayMember]?.ToString() ?? string.Empty;
                }
                else
                {
                    var property = item.GetType().GetProperty(DisplayMember);
                    return property?.GetValue(item)?.ToString() ?? string.Empty;
                }
            }
            else
            {
                return Items[index]?.ToString() ?? string.Empty;
            }
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            MouseState = MouseState.OUT;
            MeasureItem += CustomMeasureItem;
            DrawItem += CustomDrawItem;
            DropDownStyle = ComboBoxStyle.DropDownList;
            DrawMode = DrawMode.OwnerDrawVariable;

            // StartIndex'i uygula
            if (_startIndex >= 0 && _startIndex < Items.Count && SelectedIndex < 0)
            {
                SelectedIndex = _startIndex;
            }

            UpdateDisplayedText();
            RecalculateAutoSize();
            SetHeightVars();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecalculateAutoSize();
            SetHeightVars();
        }

        private void SetHeightVars()
        {
            HEIGHT = UseTallSize ? 50 : 36;
            Size = new Size(Size.Width, HEIGHT);
            LINE_Y = HEIGHT - BOTTOM_PADDING;
            ItemHeight = HEIGHT - 7;
            DropDownHeight = ItemHeight * MaxDropDownItems + 2;
        }

        public void RecalculateAutoSize()
        {
            if (!AutoResize) return;

            int w = DropDownWidth;
            int padding = SkinManager.FORM_PADDING * 3;
            int vertScrollBarWidth = (Items.Count > MaxDropDownItems) ? SystemInformation.VerticalScrollBarWidth : 0;

            using (Graphics g = CreateGraphics())
            using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
            {
                var itemsList = Items.Cast<object>().Select(item => item?.ToString() ?? string.Empty);
                foreach (string s in itemsList)
                {
                    int newWidth = nativeText.MeasureLogString(s, SkinManager.getLogFontByType(MaterialSkinManager.fontType.Subtitle1)).Width + vertScrollBarWidth + padding;
                    if (w < newWidth) w = newWidth;
                }
            }

            if (Width != w)
            {
                DropDownWidth = w;
                Width = w;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //_animationManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}