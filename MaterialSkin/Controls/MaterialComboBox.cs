namespace MaterialSkin.Controls
{
    using MaterialSkin.Animations;
    using System;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Windows.Forms;

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
        public bool UseAccent { get; set; }

        private string _hint = string.Empty;

        [Category("Material Skin"), DefaultValue(""), Localizable(true)]
        public string Hint
        {
            get { return _hint; }
            set
            {
                _hint = value;
                hasHint = !string.IsNullOrEmpty(Hint);
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
                    if (base.Items.Count > 0)
                    {
                        base.SelectedIndex = value;
                    }
                }
                catch (Exception)
                {
                    // Error caught but no action needed
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

        private readonly AnimationManager _animationManager;

        public MaterialComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            // Material Properties
            Hint = "";
            UseAccent = true;
            UseTallSize = true;
            MaxDropDownItems = 4;

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
                Invalidate();
            };

            KeyUp += (sender, args) =>
            {
                if (Enabled && DropDownStyle == ComboBoxStyle.DropDownList && (args.KeyCode == Keys.Delete || args.KeyCode == Keys.Back))
                {
                    SelectedIndex = -1;
                    Invalidate();
                }
            };
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;

            g.Clear(Parent.BackColor);
            g.FillRectangle(Enabled ? Focused ?
                SkinManager.BackgroundFocusBrush : // Focused
                MouseState == MouseState.HOVER ?
                SkinManager.BackgroundHoverBrush : // Hover
                SkinManager.BackgroundAlternativeBrush : // normal
                SkinManager.BackgroundDisabledBrush, // Disabled
                ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, LINE_Y);

            //Set color and brush
            Color selectedColor = UseAccent ?
                SkinManager.ColorScheme.AccentColor :
                SkinManager.ColorScheme.PrimaryColor;

            using (SolidBrush selectedBrush = new SolidBrush(selectedColor))
            {
                // Create and Draw the arrow
                using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    PointF topRight = new PointF(Width - 0.5f - SkinManager.FORM_PADDING, (Height >> 1) - 2.5f);
                    PointF midBottom = new PointF(Width - 4.5f - SkinManager.FORM_PADDING, (Height >> 1) + 2.5f);
                    PointF topLeft = new PointF(Width - 8.5f - SkinManager.FORM_PADDING, (Height >> 1) - 2.5f);
                    path.AddLine(topLeft, topRight);
                    path.AddLine(topRight, midBottom);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    Brush arrowBrush;
                    if (Enabled)
                    {
                        arrowBrush = DroppedDown || Focused ? selectedBrush : SkinManager.TextHighEmphasisBrush;
                    }
                    else
                    {
                        using (SolidBrush disabledBrush = new SolidBrush(DrawHelper.BlendColor(SkinManager.TextHighEmphasisColor, SkinManager.SwitchOffDisabledThumbColor, 197)))
                        {
                            arrowBrush = disabledBrush;
                            g.FillPath(arrowBrush, path);
                        }
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                        goto SkipArrowFill; // Avoid duplicate arrow fill
                    }

                    g.FillPath(arrowBrush, path);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                }

            SkipArrowFill:

                // HintText
                bool userTextPresent = SelectedIndex >= 0;
                Rectangle hintRect = new Rectangle(SkinManager.FORM_PADDING, ClientRectangle.Y, Width, LINE_Y);
                int hintTextSize = 16;

                // bottom line base
                g.FillRectangle(SkinManager.DividersAlternativeBrush, 0, LINE_Y, Width, 1);

                if (!_animationManager.IsAnimating())
                {
                    // No animation
                    if (hasHint && UseTallSize && (DroppedDown || Focused || SelectedIndex >= 0))
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
                    // Animate - Focus got/lost
                    double animationProgress = _animationManager.GetProgress();

                    // hint Animation
                    if (hasHint && UseTallSize)
                    {
                        hintRect = new Rectangle(
                            SkinManager.FORM_PADDING,
                            userTextPresent && !_animationManager.IsAnimating() ? (TEXT_SMALL_Y) : ClientRectangle.Y + (int)((TEXT_SMALL_Y - ClientRectangle.Y) * animationProgress),
                            Width,
                            userTextPresent && !_animationManager.IsAnimating() ? (TEXT_SMALL_SIZE) : (int)(LINE_Y + (TEXT_SMALL_SIZE - LINE_Y) * animationProgress));
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

                g.Clip = new Region(textRect);

                using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                {
                    // Draw user text
                    nativeText.DrawTransparentText(
                        Text,
                        SkinManager.getLogFontByType(MaterialSkinManager.fontType.Subtitle1),
                        Enabled ? SkinManager.TextHighEmphasisColor : SkinManager.TextDisabledOrHintColor,
                        textRect.Location,
                        textRect.Size,
                        NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                }

                g.ResetClip();

                // Draw hint text
                if (hasHint && (UseTallSize || string.IsNullOrEmpty(Text)))
                {
                    using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                    {
                        Color hintColor;
                        if (Enabled)
                        {
                            hintColor = DroppedDown || Focused ? selectedColor : SkinManager.TextMediumEmphasisColor;
                        }
                        else
                        {
                            hintColor = SkinManager.TextDisabledOrHintColor;
                        }

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

            string text = GetItemText(e.Index);

            using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
            {
                nativeText.DrawTransparentText(
                    text,
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
                try
                {
                    object item = Items[index];

                    if (item is DataRowView rowView)
                    {
                        if (rowView.Row.Table.Columns.Contains(DisplayMember))
                        {
                            return rowView.Row[DisplayMember]?.ToString() ?? string.Empty;
                        }
                    }
                    else
                    {
                        var prop = item.GetType().GetProperty(DisplayMember);
                        if (prop != null)
                        {
                            var propValue = prop.GetValue(item);
                            return propValue?.ToString() ?? string.Empty;
                        }
                    }
                }
                catch (Exception)
                {
                    // Fall back to default ToString() in case of any error
                }
            }

            return Items[index]?.ToString() ?? string.Empty;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            MouseState = MouseState.OUT;
            MeasureItem += CustomMeasureItem;
            DrawItem += CustomDrawItem;
            DropDownStyle = ComboBoxStyle.DropDownList;
            DrawMode = DrawMode.OwnerDrawVariable;
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
            if (!AutoResize || Items.Count == 0) return;

            int w = DropDownWidth;
            int padding = SkinManager.FORM_PADDING * 3;
            int vertScrollBarWidth = (Items.Count > MaxDropDownItems) ? SystemInformation.VerticalScrollBarWidth : 0;

            using (Graphics g = CreateGraphics())
            {
                if (g == null) return;

                using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                {
                    foreach (object item in Items)
                    {
                        if (item == null) continue;

                        string itemText = GetItemText(Items.IndexOf(item));
                        if (string.IsNullOrEmpty(itemText)) continue;

                        int newWidth = nativeText.MeasureLogString(itemText, SkinManager.getLogFontByType(MaterialSkinManager.fontType.Subtitle1)).Width + vertScrollBarWidth + padding;
                        if (w < newWidth) w = newWidth;
                    }
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
            base.Dispose(disposing);
        }
    }
}