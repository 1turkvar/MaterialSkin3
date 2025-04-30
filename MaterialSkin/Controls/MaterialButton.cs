namespace MaterialSkin.Controls
{
    using MaterialSkin.Animations;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.Globalization;
    using System.Windows.Forms;

    /// <summary>
    /// Defines the <see cref="MaterialButton" />
    /// </summary>
    public class MaterialButton : Button, IMaterialControl
    {
        private const int ICON_SIZE = 24;
        private const int MINIMUMWIDTH = 64;
        private const int MINIMUMWIDTHICONONLY = 36;
        private const int HEIGHTDEFAULT = 36;
        private const int HEIGHTDENSE = 32;

        // icons
        private TextureBrush iconsBrushes;

        /// <summary>
        /// Gets or sets the Depth
        /// </summary>
        [Browsable(false)]
        public int Depth { get; set; }

        /// <summary>
        /// Gets the SkinManager
        /// </summary>
        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        /// <summary>
        /// Gets or sets the MouseState
        /// </summary>
        [Browsable(false)]
        public MouseState MouseState { get; set; }

        public enum MaterialButtonType
        {
            Text,
            Outlined,
            Contained
        }

        public enum MaterialButtonDensity
        {
            Default,
            Dense
        }

        [Browsable(false)]
        public Color NoAccentTextColor { get; set; }

        [Category("Material Skin")]
        public bool UseAccentColor
        {
            get { return useAccentColor; }
            set { useAccentColor = value; Invalidate(); }
        }

        [Category("Material Skin")]
        /// <summary>
        /// Gets or sets a value indicating whether HighEmphasis
        /// </summary>
        public bool HighEmphasis
        {
            get { return highEmphasis; }
            set { highEmphasis = value; Invalidate(); }
        }

        [DefaultValue(true)]
        [Category("Material Skin")]
        [Description("Draw Shadows around control")]
        public bool DrawShadows
        {
            get { return drawShadows; }
            set { drawShadows = value; Invalidate(); }
        }

        [Category("Material Skin")]
        public MaterialButtonType Type
        {
            get { return type; }
            set { type = value; PreProcessIcons(); Invalidate(); }
        }

        [Category("Material Skin")]
        /// <summary>
        /// Gets or sets a value indicating button density
        /// </summary>
        public MaterialButtonDensity Density
        {
            get { return _density; }
            set
            {
                _density = value;
                if (_density == MaterialButtonDensity.Dense)
                    Size = new Size(Size.Width, HEIGHTDENSE);
                else
                    Size = new Size(Size.Width, HEIGHTDEFAULT);
                Invalidate();
            }
        }

        public enum CharacterCasingEnum
        {
            Normal,
            Lower,
            Upper,
            Title
        }

        private CharacterCasingEnum _cc;
        [Category("Behavior"), DefaultValue(CharacterCasingEnum.Upper), Description("Change capitalization of Text property")]
        public CharacterCasingEnum CharacterCasing
        {
            get => _cc;
            set
            {
                _cc = value;
                Invalidate();
            }
        }

        protected override void InitLayout()
        {
            base.InitLayout();
            Invalidate();
            LocationChanged += (sender, e) => { if (DrawShadows) Parent?.Invalidate(); };
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (drawShadows && Parent != null) AddShadowPaintEvent(Parent, drawShadowOnParent);
            if (_oldParent != null) RemoveShadowPaintEvent(_oldParent, drawShadowOnParent);
            _oldParent = Parent;
        }

        private Control _oldParent;

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Parent == null) return;
            if (Visible)
                AddShadowPaintEvent(Parent, drawShadowOnParent);
            else
                RemoveShadowPaintEvent(Parent, drawShadowOnParent);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        private bool _shadowDrawEventSubscribed = false;

        private void AddShadowPaintEvent(Control control, PaintEventHandler shadowPaintEvent)
        {
            if (_shadowDrawEventSubscribed) return;
            control.Paint += shadowPaintEvent;
            control.Invalidate();
            _shadowDrawEventSubscribed = true;
        }

        private void RemoveShadowPaintEvent(Control control, PaintEventHandler shadowPaintEvent)
        {
            if (!_shadowDrawEventSubscribed) return;
            control.Paint -= shadowPaintEvent;
            control.Invalidate();
            _shadowDrawEventSubscribed = false;
        }

        private readonly AnimationManager _hoverAnimationManager;
        private readonly AnimationManager _focusAnimationManager;
        private readonly AnimationManager _animationManager;

        /// <summary>
        /// Defines the _textSize
        /// </summary>
        private SizeF _textSize;

        /// <summary>
        /// Defines the _icon
        /// </summary>
        private Image _icon;

        private bool drawShadows;
        private bool highEmphasis;
        private bool useAccentColor;
        private MaterialButtonType type;
        private MaterialButtonDensity _density;

        [Category("Material Skin")]
        /// <summary>
        /// Gets or sets the Icon
        /// </summary>
        public Image Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                PreProcessIcons();

                if (AutoSize)
                {
                    Refresh();
                }

                Invalidate();
            }
        }

        [DefaultValue(true)]
        public override bool AutoSize
        {
            get => base.AutoSize;
            set => base.AutoSize = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialButton"/> class.
        /// </summary>
        public MaterialButton()
        {
            DrawShadows = true;
            HighEmphasis = true;
            UseAccentColor = false;
            Type = MaterialButtonType.Contained;
            Density = MaterialButtonDensity.Default;
            NoAccentTextColor = Color.Empty;
            CharacterCasing = CharacterCasingEnum.Upper;

            _animationManager = new AnimationManager(false)
            {
                Increment = 0.03,
                AnimationType = AnimationType.EaseOut
            };
            _hoverAnimationManager = new AnimationManager
            {
                Increment = 0.12,
                AnimationType = AnimationType.Linear
            };
            _focusAnimationManager = new AnimationManager
            {
                Increment = 0.12,
                AnimationType = AnimationType.Linear
            };

            SkinManager.ColorSchemeChanged += sender =>
            {
                PreProcessIcons();
            };

            SkinManager.ThemeChanged += sender =>
            {
                PreProcessIcons();
            };

            _hoverAnimationManager.OnAnimationProgress += sender => Invalidate();
            _focusAnimationManager.OnAnimationProgress += sender => Invalidate();
            _animationManager.OnAnimationProgress += sender => Invalidate();

            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            AutoSize = true;
            Margin = new Padding(4, 6, 4, 6);
            Padding = new Padding(0);
        }

        /// <summary>
        /// Gets or sets the Text
        /// </summary>
        public override string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
                if (!String.IsNullOrEmpty(value))
                    _textSize = CreateGraphics().MeasureString(value.ToUpper(), SkinManager.getFontByType(MaterialSkinManager.fontType.Button));
                else
                {
                    _textSize.Width = 0;
                    _textSize.Height = 0;
                }

                if (AutoSize)
                {
                    Refresh();
                }

                Invalidate();
            }
        }

        private void drawShadowOnParent(object sender, PaintEventArgs e)
        {
            if (Parent == null)
            {
                RemoveShadowPaintEvent((Control)sender, drawShadowOnParent);
                return;
            }

            if (!DrawShadows || Type != MaterialButtonType.Contained || Parent == null) return;

            // paint shadow on parent
            Graphics gp = e.Graphics;
            Rectangle rect = new Rectangle(Location, ClientRectangle.Size);
            gp.SmoothingMode = SmoothingMode.AntiAlias;
            DrawHelper.DrawSquareShadow(gp, rect);
        }

        private void PreProcessIcons()
        {
            if (Icon == null) return;

            int newWidth, newHeight;
            //Resize icon if greater than ICON_SIZE
            if (Icon.Width > ICON_SIZE || Icon.Height > ICON_SIZE)
            {
                //calculate aspect ratio
                float aspect = Icon.Width / (float)Icon.Height;

                //calculate new dimensions based on aspect ratio
                newWidth = (int)(ICON_SIZE * aspect);
                newHeight = (int)(newWidth / aspect);

                //if one of the two dimensions exceed the box dimensions
                if (newWidth > ICON_SIZE || newHeight > ICON_SIZE)
                {
                    //depending on which of the two exceeds the box dimensions set it as the box dimension and calculate the other one based on the aspect ratio
                    if (newWidth > newHeight)
                    {
                        newWidth = ICON_SIZE;
                        newHeight = (int)(newWidth / aspect);
                    }
                    else
                    {
                        newHeight = ICON_SIZE;
                        newWidth = (int)(newHeight * aspect);
                    }
                }
            }
            else
            {
                newWidth = Icon.Width;
                newHeight = Icon.Height;
            }

            using (Bitmap IconResized = new Bitmap(Icon, newWidth, newHeight))
            {
                // Calculate lightness and color
                float l = (SkinManager.Theme == MaterialSkinManager.Themes.LIGHT && (highEmphasis == false || Enabled == false || Type != MaterialButtonType.Contained)) ? 0f : 1.5f;

                // Create matrices
                float[][] matrixGray = {
                        new float[] {   0,   0,   0,   0,  0}, // Red scale factor
                        new float[] {   0,   0,   0,   0,  0}, // Green scale factor
                        new float[] {   0,   0,   0,   0,  0}, // Blue scale factor
                        new float[] {   0,   0,   0, Enabled ? .7f : .3f,  0}, // alpha scale factor
                        new float[] {   l,   l,   l,   0,  1}};// offset

                ColorMatrix colorMatrixGray = new ColorMatrix(matrixGray);

                using (ImageAttributes grayImageAttributes = new ImageAttributes())
                {
                    // Set color matrices
                    grayImageAttributes.SetColorMatrix(colorMatrixGray, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    // Image Rect
                    Rectangle destRect = new Rectangle(0, 0, ICON_SIZE, ICON_SIZE);

                    // Create a pre-processed copy of the image (GRAY)
                    using (Bitmap bgray = new Bitmap(destRect.Width, destRect.Height))
                    {
                        using (Graphics gGray = Graphics.FromImage(bgray))
                        {
                            gGray.DrawImage(IconResized,
                                new Point[] {
                                            new Point(0, 0),
                                            new Point(destRect.Width, 0),
                                            new Point(0, destRect.Height),
                                },
                                destRect, GraphicsUnit.Pixel, grayImageAttributes);
                        }

                        // added processed image to brush for drawing
                        if (iconsBrushes != null)
                        {
                            iconsBrushes.Dispose();
                        }
                        iconsBrushes = new TextureBrush(bgray);
                        iconsBrushes.WrapMode = WrapMode.Clamp;

                        // Translate the brushes to the correct positions
                        var iconRect = new Rectangle(8, (Height / 2 - ICON_SIZE / 2), ICON_SIZE, ICON_SIZE);
                        iconsBrushes.TranslateTransform(iconRect.X + iconRect.Width / 2 - IconResized.Width / 2,
                                                        iconRect.Y + iconRect.Height / 2 - IconResized.Height / 2);
                    }
                }
            }
        }

        /// <summary>
        /// Özel çizim işlemlerini gerçekleştirir
        /// </summary>
        /// <param name="pevent">Çizim olayı argümanları</param>
        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            double hoverAnimProgress = _hoverAnimationManager.GetProgress();
            double focusAnimProgress = _focusAnimationManager.GetProgress();

            g.Clear(Parent.BackColor);

            // Button rect ve path oluşturma
            RectangleF buttonRectF = new RectangleF(ClientRectangle.Location, ClientRectangle.Size);
            buttonRectF.X -= 0.5f;
            buttonRectF.Y -= 0.5f;

            using (GraphicsPath buttonPath = DrawHelper.CreateRoundRect(buttonRectF, 4))
            {
                // Button gölgesini çiz
                if (DrawShadows)
                {
                    DrawHelper.DrawSquareShadow(g, ClientRectangle);
                }

                // Button arkaplanını çiz
                DrawButtonBackground(g, buttonPath);

                // Hover efektini çiz
                DrawHoverEffect(g, buttonPath, hoverAnimProgress);

                // Focus efektini çiz
                DrawFocusEffect(g, buttonPath, focusAnimProgress);

                // Outlined tipinde kenarlık çiz
                if (Type == MaterialButtonType.Outlined)
                {
                    DrawOutline(g, buttonPath, buttonRectF);
                }

                // Ripple efektini çiz
                DrawRippleEffect(g, buttonRectF);

                // Metni çiz
                DrawButtonText(g);

                // İkonu çiz
                DrawButtonIcon(g);
            }
        }

        private void DrawButtonBackground(Graphics g, GraphicsPath buttonPath)
        {
            if (Type == MaterialButtonType.Contained)
            {
                // Devre dışı
                if (!Enabled)
                {
                    using (SolidBrush disabledBrush = new SolidBrush(DrawHelper.BlendColor(Parent.BackColor, SkinManager.BackgroundDisabledColor, SkinManager.BackgroundDisabledColor.A)))
                    {
                        g.FillPath(disabledBrush, buttonPath);
                    }
                }
                // Yüksek vurgu
                else if (HighEmphasis)
                {
                    g.FillPath(UseAccentColor ? SkinManager.ColorScheme.AccentBrush : SkinManager.ColorScheme.PrimaryBrush, buttonPath);
                }
                // Normal
                else
                {
                    using (SolidBrush normalBrush = new SolidBrush(SkinManager.BackgroundColor))
                    {
                        g.FillPath(normalBrush, buttonPath);
                    }
                }
            }
            else
            {
                g.Clear(Parent.BackColor);
            }
        }

        private void DrawHoverEffect(Graphics g, GraphicsPath buttonPath, double hoverAnimProgress)
        {
            if (hoverAnimProgress > 0)
            {
                Color hoverColor = GetEffectColor(true, hoverAnimProgress);
                using (SolidBrush hoverBrush = new SolidBrush(hoverColor))
                {
                    g.FillPath(hoverBrush, buttonPath);
                }
            }
        }

        private void DrawFocusEffect(Graphics g, GraphicsPath buttonPath, double focusAnimProgress)
        {
            if (focusAnimProgress > 0)
            {
                Color focusColor = GetEffectColor(false, focusAnimProgress);
                using (SolidBrush focusBrush = new SolidBrush(focusColor))
                {
                    g.FillPath(focusBrush, buttonPath);
                }
            }
        }

        private Color GetEffectColor(bool isHover, double animProgress)
        {
            int alpha = (int)(HighEmphasis && Type == MaterialButtonType.Contained ?
                             animProgress * 80 :
                             animProgress * (isHover ? SkinManager.BackgroundHoverColor.A : SkinManager.BackgroundFocusColor.A));

            Color baseColor;

            if (UseAccentColor)
            {
                if (HighEmphasis && Type == MaterialButtonType.Contained)
                    baseColor = SkinManager.ColorScheme.AccentColor.Lighten(0.5f);
                else
                    baseColor = SkinManager.ColorScheme.AccentColor;
            }
            else
            {
                if (Type == MaterialButtonType.Contained && HighEmphasis)
                    baseColor = SkinManager.ColorScheme.LightPrimaryColor;
                else
                    baseColor = SkinManager.ColorScheme.PrimaryColor;
            }

            return Color.FromArgb(alpha, baseColor.RemoveAlpha());
        }

        private void DrawOutline(Graphics g, GraphicsPath buttonPath, RectangleF buttonRectF)
        {
            using (Pen outlinePen = new Pen(Enabled ? SkinManager.DividersAlternativeColor : SkinManager.DividersColor, 1))
            {
                buttonRectF.X += 0.5f;
                buttonRectF.Y += 0.5f;
                g.DrawPath(outlinePen, buttonPath);
            }
        }

        private void DrawRippleEffect(Graphics g, RectangleF buttonRectF)
        {
            if (_animationManager.IsAnimating())
            {
                g.Clip = new Region(buttonRectF);
                try
                {
                    for (var i = 0; i < _animationManager.GetAnimationCount(); i++)
                    {
                        var animationValue = _animationManager.GetProgress(i);
                        var animationSource = _animationManager.GetSource(i);

                        Color rippleColor = GetRippleColor();
                        using (Brush rippleBrush = new SolidBrush(Color.FromArgb((int)(100 - (animationValue * 100)), rippleColor)))
                        {
                            var rippleSize = (int)(animationValue * Width * 2);
                            g.FillEllipse(rippleBrush, new Rectangle(animationSource.X - rippleSize / 2, animationSource.Y - rippleSize / 2, rippleSize, rippleSize));
                        }
                    }
                }
                finally
                {
                    g.ResetClip();
                }
            }
        }

        private Color GetRippleColor()
        {
            if (Type == MaterialButtonType.Contained && HighEmphasis)
            {
                return UseAccentColor ? SkinManager.ColorScheme.AccentColor.Lighten(0.5f) : SkinManager.ColorScheme.LightPrimaryColor;
            }
            else
            {
                return UseAccentColor ?
                       SkinManager.ColorScheme.AccentColor :
                       (SkinManager.Theme == MaterialSkinManager.Themes.LIGHT ?
                       SkinManager.ColorScheme.PrimaryColor :
                       SkinManager.ColorScheme.LightPrimaryColor);
            }
        }

        private void DrawButtonText(Graphics g)
        {
            // Skip if text is empty
            if (string.IsNullOrEmpty(Text))
                return;

            // Text alanını hesapla
            var textRect = ClientRectangle;
            if (Icon != null)
            {
                textRect.Width -= 8 + ICON_SIZE + 4 + 8;
                textRect.X += 8 + ICON_SIZE + 4;
            }

            // Text rengini belirle
            Color textColor = GetTextColor();

            // Metni çiz
            using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
            {
                string formattedText = FormatText(Text);
                NativeText.DrawMultilineTransparentText(
                    formattedText,
                    SkinManager.getLogFontByType(MaterialSkinManager.fontType.Button),
                    textColor,
                    textRect.Location,
                    textRect.Size,
                    NativeTextRenderer.TextAlignFlags.Center | NativeTextRenderer.TextAlignFlags.Middle);
            }
        }

        private Color GetTextColor()
        {
            if (!Enabled)
                return SkinManager.TextDisabledOrHintColor;

            if (HighEmphasis)
            {
                if (Type == MaterialButtonType.Text || Type == MaterialButtonType.Outlined)
                {
                    if (UseAccentColor)
                        return SkinManager.ColorScheme.AccentColor;

                    return NoAccentTextColor == Color.Empty ?
                          SkinManager.ColorScheme.PrimaryColor :
                          NoAccentTextColor;
                }
                return SkinManager.ColorScheme.TextColor;
            }

            return SkinManager.TextHighEmphasisColor;
        }

        private string FormatText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            switch (CharacterCasing)
            {
                case CharacterCasingEnum.Upper:
                    return text.ToUpper();
                case CharacterCasingEnum.Lower:
                    return text.ToLower();
                case CharacterCasingEnum.Title:
                    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
                default:
                    return text;
            }
        }

        private void DrawButtonIcon(Graphics g)
        {
            if (Icon == null || iconsBrushes == null)
                return;

            var iconRect = new Rectangle(8, (Height / 2) - (ICON_SIZE / 2), ICON_SIZE, ICON_SIZE);

            if (string.IsNullOrEmpty(Text))
            {
                // İkonu ortala
                iconRect.X += 2;
            }

            g.FillRectangle(iconsBrushes, iconRect);
        }

        /// <summary>
        /// The GetPreferredSize
        /// </summary>
        /// <returns>The <see cref="Size"/></returns>
        private Size GetPreferredSize()
        {
            return GetPreferredSize(Size);
        }

        /// <summary>
        /// The GetPreferredSize
        /// </summary>
        /// <param name="proposedSize">The proposedSize<see cref="Size"/></param>
        /// <returns>The <see cref="Size"/></returns>
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size s = base.GetPreferredSize(proposedSize);

            // Provides extra space for proper padding for content
            var extra = 16;

            if (Icon != null)
            {
                // 24 is for icon size
                // 4 is for the space between icon & text
                extra += ICON_SIZE + 4;
            }

            if (AutoSize)
            {
                s.Width = (int)Math.Ceiling(_textSize.Width);
                s.Width += extra;
                s.Height = _density == MaterialButtonDensity.Dense ? HEIGHTDENSE : HEIGHTDEFAULT;
            }
            else
            {
                s.Width += extra;
                s.Height = _density == MaterialButtonDensity.Dense ? HEIGHTDENSE : HEIGHTDEFAULT;
            }

            if (Icon != null && string.IsNullOrEmpty(Text) && s.Width < MINIMUMWIDTHICONONLY)
                s.Width = MINIMUMWIDTHICONONLY;
            else if (s.Width < MINIMUMWIDTH)
                s.Width = MINIMUMWIDTH;

            return s;
        }

        /// <summary>
        /// The OnCreateControl
        /// </summary>
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            // before checking DesignMode property, as long as we need see Icon in proper position
            Resize += (sender, args) => { PreProcessIcons(); Invalidate(); };

            if (DesignMode)
            {
                return;
            }

            MouseState = MouseState.OUT;
            MouseEnter += (sender, args) =>
            {
                MouseState = MouseState.HOVER;
                _hoverAnimationManager.StartNewAnimation(AnimationDirection.In);
                Invalidate();
            };
            MouseLeave += (sender, args) =>
            {
                MouseState = MouseState.OUT;
                _hoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
                Invalidate();
            };
            MouseDown += (sender, args) =>
            {
                if (args.Button == MouseButtons.Left)
                {
                    MouseState = MouseState.DOWN;

                    _animationManager.StartNewAnimation(AnimationDirection.In, args.Location);
                    Invalidate();
                }
            };
            MouseUp += (sender, args) =>
            {
                MouseState = MouseState.HOVER;

                Invalidate();
            };

            GotFocus += (sender, args) =>
            {
                _focusAnimationManager.StartNewAnimation(AnimationDirection.In);
                Invalidate();
            };
            LostFocus += (sender, args) =>
            {
                MouseState = MouseState.OUT;
                _focusAnimationManager.StartNewAnimation(AnimationDirection.Out);
                Invalidate();
            };

            PreviewKeyDown += (object sender, PreviewKeyDownEventArgs e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    _animationManager.StartNewAnimation(AnimationDirection.In, new Point(ClientRectangle.Width >> 1, ClientRectangle.Height >> 1));
                    Invalidate();
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (iconsBrushes != null)
                {
                    iconsBrushes.Dispose();
                    iconsBrushes = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}