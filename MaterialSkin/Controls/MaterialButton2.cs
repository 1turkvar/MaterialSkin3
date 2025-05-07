using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using MaterialSkin.Animations;
using MaterialSkin.Controls;

namespace MaterialSkin.Controls
{
    /// <summary>
    /// Material Button with Bootstrap style options
    /// </summary>
    public class MaterialButton2 : Button, IMaterialControl
    {
        // Constants
        private const int ICON_SIZE = 24;
        private const int MINIMUMWIDTH = 64;
        private const int MINIMUMWIDTHICONONLY = 36;
        private const int HEIGHTDEFAULT = 36;
        private const int HEIGHTDENSE = 32;

        // Icons
        private TextureBrush iconsBrushes;

        #region IMaterialControl Implementation

        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        #endregion

        #region Properties

        // Button type enum
        public enum MaterialButtonType
        {
            Text,
            Outlined,
            Contained
        }

        // Button density enum
        public enum MaterialButtonDensity
        {
            Default,
            Dense
        }

        // Bootstrap style enum (matching Bootstrap color variants)
        public enum BootstrapStyle
        {
            Primary,
            Secondary,
            Success,
            Danger,
            Warning,
            Info,
            Light,
            Dark
        }

        // Text casing enum
        public enum CharacterCasingEnum
        {
            Normal,
            Lower,
            Upper,
            Title
        }

        [Category("Material Skin")]
        public MaterialButtonType Type
        {
            get { return _type; }
            set { _type = value; PreProcessIcons(); Invalidate(); }
        }

        [Category("Material Skin")]
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

        [Category("Material Skin")]
        public BootstrapStyle BootstrapStyleType
        {
            get { return _bootstrapStyle; }
            set { _bootstrapStyle = value; Invalidate(); }
        }

        [Category("Material Skin")]
        [DefaultValue(true)]
        public bool HighEmphasis
        {
            get { return _highEmphasis; }
            set { _highEmphasis = value; Invalidate(); }
        }

        [DefaultValue(true)]
        [Category("Material Skin")]
        [Description("Draw Shadows around control")]
        public bool DrawShadows
        {
            get { return _drawShadows; }
            set { _drawShadows = value; Invalidate(); }
        }

        [Browsable(false)]
        public Color NoAccentTextColor { get; set; }

        [Category("Material Skin")]
        [DefaultValue(CharacterCasingEnum.Upper)]
        [Description("Change capitalization of Text property")]
        public CharacterCasingEnum CharacterCasing
        {
            get => _characterCasing;
            set
            {
                _characterCasing = value;
                Invalidate();
            }
        }

        [Category("Material Skin")]
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

        #endregion

        #region Private Fields

        private readonly AnimationManager _hoverAnimationManager;
        private readonly AnimationManager _focusAnimationManager;
        private readonly AnimationManager _animationManager;

        private SizeF _textSize;
        private Image _icon;
        private MaterialButtonType _type;
        private MaterialButtonDensity _density;
        private BootstrapStyle _bootstrapStyle;
        private bool _drawShadows;
        private bool _highEmphasis;
        private CharacterCasingEnum _characterCasing;
        private Control _oldParent;
        private bool _shadowDrawEventSubscribed = false;

        // Bootstrap color definitions
        private readonly Color[] _bootstrapColors = new Color[]
        {
            Color.FromArgb(0, 123, 255),   // Primary
            Color.FromArgb(108, 117, 125), // Secondary
            Color.FromArgb(40, 167, 69),   // Success 
            Color.FromArgb(220, 53, 69),   // Danger
            Color.FromArgb(255, 193, 7),   // Warning
            Color.FromArgb(23, 162, 184),  // Info
            Color.FromArgb(248, 249, 250), // Light
            Color.FromArgb(52, 58, 64)     // Dark
        };

        // Bootstrap lighter colors for effects
        private readonly Color[] _bootstrapLightColors = new Color[]
        {
            Color.FromArgb(0, 123, 255).Lighten(0.3f),   // Primary
            Color.FromArgb(108, 117, 125).Lighten(0.3f), // Secondary
            Color.FromArgb(40, 167, 69).Lighten(0.3f),   // Success
            Color.FromArgb(220, 53, 69).Lighten(0.3f),   // Danger
            Color.FromArgb(255, 193, 7).Lighten(0.3f),   // Warning
            Color.FromArgb(23, 162, 184).Lighten(0.3f),  // Info
            Color.FromArgb(248, 249, 250).Darken(0.1f),  // Light
            Color.FromArgb(52, 58, 64).Lighten(0.3f)     // Dark
        };

        // Text colors for each bootstrap style (for outlined and text buttons)
        private readonly Color[] _bootstrapTextColors = new Color[]
        {
            Color.FromArgb(0, 123, 255),   // Primary
            Color.FromArgb(108, 117, 125), // Secondary
            Color.FromArgb(40, 167, 69),   // Success
            Color.FromArgb(220, 53, 69),   // Danger
            Color.FromArgb(255, 193, 7),   // Warning
            Color.FromArgb(23, 162, 184),  // Info
            Color.FromArgb(248, 249, 250), // Light
            Color.FromArgb(52, 58, 64)     // Dark
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize a new instance of BootstrapMaterialButton
        /// </summary>
        public MaterialButton2()
        {
            _drawShadows = true;
            _highEmphasis = true;
            _type = MaterialButtonType.Contained;
            _density = MaterialButtonDensity.Default;
            _bootstrapStyle = BootstrapStyle.Primary;
            _characterCasing = CharacterCasingEnum.Upper;
            NoAccentTextColor = Color.Empty;

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

        #endregion

        #region Event Handlers

        protected override void InitLayout()
        {
            base.InitLayout();
            Invalidate();
            LocationChanged += (sender, e) => { if (DrawShadows) Parent?.Invalidate(); };
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (_drawShadows && Parent != null) AddShadowPaintEvent(Parent, DrawShadowOnParent);
            if (_oldParent != null) RemoveShadowPaintEvent(_oldParent, DrawShadowOnParent);
            _oldParent = Parent;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Parent == null) return;
            if (Visible)
                AddShadowPaintEvent(Parent, DrawShadowOnParent);
            else
                RemoveShadowPaintEvent(Parent, DrawShadowOnParent);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            // Update icons when resizing
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

        #endregion

        #region Shadow Methods

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

        private void DrawShadowOnParent(object sender, PaintEventArgs e)
        {
            if (Parent == null)
            {
                RemoveShadowPaintEvent((Control)sender, DrawShadowOnParent);
                return;
            }

            if (!DrawShadows || Type != MaterialButtonType.Contained || Parent == null) return;

            // Paint shadow on parent
            Graphics gp = e.Graphics;
            Rectangle rect = new Rectangle(Location, ClientRectangle.Size);
            gp.SmoothingMode = SmoothingMode.AntiAlias;
            DrawHelper.DrawSquareShadow(gp, rect);
        }

        #endregion

        #region Icon Processing

        private void PreProcessIcons()
        {
            if (Icon == null) return;

            int newWidth, newHeight;
            // Resize icon if greater than ICON_SIZE
            if (Icon.Width > ICON_SIZE || Icon.Height > ICON_SIZE)
            {
                // Calculate aspect ratio
                float aspect = Icon.Width / (float)Icon.Height;

                // Calculate new dimensions based on aspect ratio
                newWidth = (int)(ICON_SIZE * aspect);
                newHeight = (int)(newWidth / aspect);

                // If one of the dimensions exceeds the box dimensions
                if (newWidth > ICON_SIZE || newHeight > ICON_SIZE)
                {
                    // Depending on which exceeds the box dimensions, set it as the box dimension and calculate the other based on aspect ratio
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
                float l = (SkinManager.Theme == MaterialSkinManager.Themes.LIGHT && (_highEmphasis == false || Enabled == false || Type != MaterialButtonType.Contained)) ? 0f : 1.5f;

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

                        // Add processed image to brush for drawing
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

        #endregion

        #region Drawing Methods

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            double hoverAnimProgress = _hoverAnimationManager.GetProgress();
            double focusAnimProgress = _focusAnimationManager.GetProgress();

            g.Clear(Parent.BackColor);

            // Create button rect and path
            RectangleF buttonRectF = new RectangleF(ClientRectangle.Location, ClientRectangle.Size);
            buttonRectF.X -= 0.5f;
            buttonRectF.Y -= 0.5f;

            using (GraphicsPath buttonPath = DrawHelper.CreateRoundRect(buttonRectF, 4))
            {
                // Draw button shadow
                if (_drawShadows)
                {
                    DrawHelper.DrawSquareShadow(g, ClientRectangle);
                }

                // Draw button background
                DrawButtonBackground(g, buttonPath);

                // Draw hover effect
                DrawHoverEffect(g, buttonPath, hoverAnimProgress);

                // Draw focus effect
                DrawFocusEffect(g, buttonPath, focusAnimProgress);

                // Draw outline for outlined buttons
                if (_type == MaterialButtonType.Outlined)
                {
                    DrawOutline(g, buttonPath, buttonRectF);
                }

                // Draw ripple effect
                DrawRippleEffect(g, buttonRectF);

                // Draw text
                DrawButtonText(g);

                // Draw icon
                DrawButtonIcon(g);
            }
        }

        private void DrawButtonBackground(Graphics g, GraphicsPath buttonPath)
        {
            if (_type == MaterialButtonType.Contained)
            {
                // Disabled
                if (!Enabled)
                {
                    using (SolidBrush disabledBrush = new SolidBrush(DrawHelper.BlendColor(Parent.BackColor, SkinManager.BackgroundDisabledColor, SkinManager.BackgroundDisabledColor.A)))
                    {
                        g.FillPath(disabledBrush, buttonPath);
                    }
                }
                // High emphasis - use bootstrap color
                else if (_highEmphasis)
                {
                    using (SolidBrush primaryBrush = new SolidBrush(_bootstrapColors[(int)_bootstrapStyle]))
                    {
                        g.FillPath(primaryBrush, buttonPath);
                    }
                }
                // Normal - use background color
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
            int alpha = (int)(_highEmphasis && _type == MaterialButtonType.Contained ?
                             animProgress * 80 :
                             animProgress * (isHover ? SkinManager.BackgroundHoverColor.A : SkinManager.BackgroundFocusColor.A));

            Color baseColor = _bootstrapLightColors[(int)_bootstrapStyle];

            return Color.FromArgb(alpha, baseColor.RemoveAlpha());
        }

        private void DrawOutline(Graphics g, GraphicsPath buttonPath, RectangleF buttonRectF)
        {
            using (Pen outlinePen = new Pen(Enabled ? _bootstrapColors[(int)_bootstrapStyle] : SkinManager.DividersColor, 1))
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
            if (_type == MaterialButtonType.Contained && _highEmphasis)
            {
                return _bootstrapLightColors[(int)_bootstrapStyle];
            }
            else
            {
                return _bootstrapColors[(int)_bootstrapStyle];
            }
        }

        private void DrawButtonText(Graphics g)
        {
            // Skip if text is empty
            if (string.IsNullOrEmpty(Text))
                return;

            // Calculate text area
            var textRect = ClientRectangle;
            if (Icon != null)
            {
                textRect.Width -= 8 + ICON_SIZE + 4 + 8;
                textRect.X += 8 + ICON_SIZE + 4;
            }

            // Set text color
            Color textColor = GetTextColor();

            // Draw text
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

            if (_highEmphasis)
            {
                if (_type == MaterialButtonType.Text || _type == MaterialButtonType.Outlined)
                {
                    return _bootstrapTextColors[(int)_bootstrapStyle];
                }

                // For light and dark variants in contained buttons, adjust text color
                if (_bootstrapStyle == BootstrapStyle.Light)
                    return Color.FromArgb(33, 37, 41); // Dark text for light background

                return Color.White; // White text for all other contained buttons
            }

            return _bootstrapTextColors[(int)_bootstrapStyle];
        }

        private string FormatText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            switch (_characterCasing)
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
                // Center icon
                iconRect.X += 2;
            }

            g.FillRectangle(iconsBrushes, iconRect);
        }

        #endregion

        #region Size Calculation

        /// <summary>
        /// Calculate preferred size
        /// </summary>
        private Size GetPreferredSize()
        {
            return GetPreferredSize(Size);
        }

        /// <summary>
        /// Calculate preferred size
        /// </summary>
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size s = base.GetPreferredSize(proposedSize);

            // Add extra space for proper padding
            var extra = 16;

            if (Icon != null)
            {
                // 24 for icon size
                // 4 for space between icon & text
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

        #endregion

        #region Cleanup

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

        #endregion
    }
}