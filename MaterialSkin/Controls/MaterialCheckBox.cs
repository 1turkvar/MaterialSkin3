namespace MaterialSkin.Controls
{
    using MaterialSkin.Animations;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;
    using System.Windows.Forms;

    public class MaterialCheckbox : CheckBox, IMaterialControl
    {
        #region Public properties
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        [Browsable(false)]
        public Point MouseLocation { get; set; }

        private bool _ripple;

        [Category("Appearance")]
        public bool Ripple
        {
            get { return _ripple; }
            set
            {
                _ripple = value;
                AutoSize = AutoSize; // Updates the bounds by resetting the AutoSize property

                if (value)
                {
                    Margin = new Padding(0);
                }

                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly { get; set; }
        #endregion

        #region Private fields
        private readonly AnimationManager _checkAM;
        private readonly AnimationManager _rippleAM;
        private readonly AnimationManager _hoverAM;
        private const int HEIGHT_RIPPLE = 37;
        private const int HEIGHT_NO_RIPPLE = 20;
        private const int TEXT_OFFSET = 26;
        private const int CHECKBOX_SIZE = 18;
        private const int CHECKBOX_SIZE_HALF = CHECKBOX_SIZE / 2;
        private int _boxOffset;
        private static readonly Point[] CheckmarkLine = { new Point(3, 8), new Point(7, 12), new Point(14, 5) };
        private bool _hovered = false;
        private CheckState _oldCheckState;
        // Store the checkmark bitmap to avoid recreating it every paint cycle
        private Bitmap _checkMarkBitmap;
        #endregion

        #region Constructor
        public MaterialCheckbox()
        {
            _checkAM = new AnimationManager
            {
                AnimationType = AnimationType.EaseInOut,
                Increment = 0.05
            };
            _hoverAM = new AnimationManager(true)
            {
                AnimationType = AnimationType.Linear,
                Increment = 0.10
            };
            _rippleAM = new AnimationManager(false)
            {
                AnimationType = AnimationType.Linear,
                Increment = 0.10,
                SecondaryIncrement = 0.08
            };

            CheckedChanged += (sender, args) =>
            {
                if (ReadOnly)
                {
                    // Prevent state change if ReadOnly is true
                    CheckState = _oldCheckState;
                    return;
                }

                if (Ripple)
                    _checkAM.StartNewAnimation(Checked ? AnimationDirection.In : AnimationDirection.Out);
            };

            _checkAM.OnAnimationProgress += sender => Invalidate();
            _hoverAM.OnAnimationProgress += sender => Invalidate();
            _rippleAM.OnAnimationProgress += sender => Invalidate();

            Ripple = true;
            Height = HEIGHT_RIPPLE;
            MouseLocation = new Point(-1, -1);
        }
        #endregion

        #region Overridden events
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _boxOffset = HEIGHT_RIPPLE / 2 - 9;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size strSize;

            using (NativeTextRenderer nativeText = new NativeTextRenderer(CreateGraphics()))
            {
                strSize = nativeText.MeasureLogString(Text, SkinManager.getLogFontByType(MaterialSkinManager.fontType.Body1));
            }

            int width = _boxOffset + TEXT_OFFSET + strSize.Width;
            return Ripple ? new Size(width, HEIGHT_RIPPLE) : new Size(width, HEIGHT_NO_RIPPLE);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Clear control
            g.Clear(Parent.BackColor);

            int checkboxCenter = _boxOffset + CHECKBOX_SIZE_HALF - 1;
            Point animationSource = new Point(checkboxCenter, checkboxCenter);
            double animationProgress = _checkAM.GetProgress();

            int colorAlpha = Enabled ? (int)(animationProgress * 255.0) : SkinManager.CheckBoxOffDisabledColor.A;
            int backgroundAlpha = Enabled ? (int)(SkinManager.CheckboxOffColor.A * (1.0 - animationProgress)) : SkinManager.CheckBoxOffDisabledColor.A;
            int rippleHeight = (HEIGHT_RIPPLE % 2 == 0) ? HEIGHT_RIPPLE - 3 : HEIGHT_RIPPLE - 2;

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(colorAlpha, Enabled ? SkinManager.ColorScheme.AccentColor : SkinManager.CheckBoxOffDisabledColor)))
            using (Pen pen = new Pen(brush.Color, 2))
            {
                // Draw hover animation
                if (Ripple)
                {
                    double animationValue = _hoverAM.IsAnimating() ? _hoverAM.GetProgress() : _hovered ? 1 : 0;
                    int rippleSize = (int)(rippleHeight * (0.7 + (0.3 * animationValue)));

                    using (SolidBrush rippleBrush = new SolidBrush(Color.FromArgb((int)(40 * animationValue),
                        !Checked ? (SkinManager.Theme == MaterialSkinManager.Themes.LIGHT ? Color.Black : Color.White) : brush.Color)))
                    {
                        g.FillEllipse(rippleBrush, new Rectangle(animationSource.X - rippleSize / 2, animationSource.Y - rippleSize / 2, rippleSize, rippleSize));
                    }
                }

                // Draw ripple animation
                if (Ripple && _rippleAM.IsAnimating())
                {
                    for (int i = 0; i < _rippleAM.GetAnimationCount(); i++)
                    {
                        double animationValue = _rippleAM.GetProgress(i);
                        int rippleSize = (_rippleAM.GetDirection(i) == AnimationDirection.InOutIn)
                            ? (int)(rippleHeight * (0.7 + (0.3 * animationValue)))
                            : rippleHeight;

                        using (SolidBrush rippleBrush = new SolidBrush(Color.FromArgb(
                            (int)((animationValue * 40)),
                            !Checked
                                ? (SkinManager.Theme == MaterialSkinManager.Themes.LIGHT ? Color.Black : Color.White)
                                : brush.Color)))
                        {
                            g.FillEllipse(rippleBrush, new Rectangle(
                                animationSource.X - rippleSize / 2,
                                animationSource.Y - rippleSize / 2,
                                rippleSize,
                                rippleSize));
                        }
                    }
                }

                Rectangle checkMarkLineFill = new Rectangle(_boxOffset, _boxOffset, (int)(CHECKBOX_SIZE * animationProgress), CHECKBOX_SIZE);
                using (GraphicsPath checkmarkPath = DrawHelper.CreateRoundRect(_boxOffset - 0.5f, _boxOffset - 0.5f, CHECKBOX_SIZE, CHECKBOX_SIZE, 1))
                {
                    if (Enabled)
                    {
                        using (Pen pen2 = new Pen(DrawHelper.BlendColor(
                            Parent.BackColor,
                            Enabled ? SkinManager.CheckboxOffColor : SkinManager.CheckBoxOffDisabledColor,
                            backgroundAlpha), 2))
                        {
                            g.DrawPath(pen2, checkmarkPath);
                        }

                        g.DrawPath(pen, checkmarkPath);
                        g.FillPath(brush, checkmarkPath);
                    }
                    else
                    {
                        if (Checked)
                            g.FillPath(brush, checkmarkPath);
                        else
                            g.DrawPath(pen, checkmarkPath);
                    }

                    // Create checkmark bitmap if not already created
                    if (_checkMarkBitmap == null || _checkMarkBitmap.Width != CHECKBOX_SIZE || _checkMarkBitmap.Height != CHECKBOX_SIZE)
                    {
                        // Dispose previous bitmap if exists
                        if (_checkMarkBitmap != null)
                        {
                            _checkMarkBitmap.Dispose();
                        }
                        _checkMarkBitmap = CreateCheckMarkBitmap();
                    }

                    g.DrawImageUnscaledAndClipped(_checkMarkBitmap, checkMarkLineFill);
                }

                // Draw checkbox text
                using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
                {
                    Rectangle textLocation = new Rectangle(_boxOffset + TEXT_OFFSET, 0, Width - (_boxOffset + TEXT_OFFSET), HEIGHT_RIPPLE);
                    nativeText.DrawTransparentText(
                        Text,
                        SkinManager.getLogFontByType(MaterialSkinManager.fontType.Body1),
                        Enabled ? SkinManager.TextHighEmphasisColor : SkinManager.TextDisabledOrHintColor,
                        textLocation.Location,
                        textLocation.Size,
                        NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                }
            }
        }

        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set
            {
                base.AutoSize = value;
                if (value)
                {
                    Size = new Size(10, 10);
                }
            }
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            if (DesignMode) return;

            MouseState = MouseState.OUT;

            GotFocus += (sender, args) =>
            {
                if (Ripple && !_hovered)
                {
                    _hoverAM.StartNewAnimation(AnimationDirection.In, new object[] { Checked });
                    _hovered = true;
                }
            };

            LostFocus += (sender, args) =>
            {
                if (Ripple && _hovered)
                {
                    _hoverAM.StartNewAnimation(AnimationDirection.Out, new object[] { Checked });
                    _hovered = false;
                }
            };

            MouseEnter += (sender, args) =>
            {
                MouseState = MouseState.HOVER;
                _oldCheckState = CheckState;

                if (Ripple && !_hovered)
                {
                    _hoverAM.StartNewAnimation(AnimationDirection.In, new object[] { Checked });
                    _hovered = true;
                }
            };

            MouseLeave += (sender, args) =>
            {
                MouseLocation = new Point(-1, -1);
                MouseState = MouseState.OUT;

                if (Ripple && _hovered)
                {
                    _hoverAM.StartNewAnimation(AnimationDirection.Out, new object[] { Checked });
                    _hovered = false;
                }
            };

            MouseDown += (sender, args) =>
            {
                MouseState = MouseState.DOWN;
                if (Ripple)
                {
                    _rippleAM.SecondaryIncrement = 0;
                    _rippleAM.StartNewAnimation(AnimationDirection.InOutIn, new object[] { Checked });
                }
                _oldCheckState = CheckState;
            };

            KeyDown += (sender, args) =>
            {
                if (Ripple && (args.KeyCode == Keys.Space) && _rippleAM.GetAnimationCount() == 0)
                {
                    _rippleAM.SecondaryIncrement = 0;
                    _rippleAM.StartNewAnimation(AnimationDirection.InOutIn, new object[] { Checked });
                }
                _oldCheckState = CheckState;
            };

            MouseUp += (sender, args) =>
            {
                if (Ripple)
                {
                    MouseState = MouseState.HOVER;
                    _rippleAM.SecondaryIncrement = 0.08;

                    // Don't start a new animation if we're already hovering
                    if (_hovered)
                    {
                        _hoverAM.StartNewAnimation(AnimationDirection.Out, new object[] { Checked });
                        _hovered = false;
                    }
                }

                if (ReadOnly) CheckState = _oldCheckState;
            };

            KeyUp += (sender, args) =>
            {
                if (Ripple && (args.KeyCode == Keys.Space))
                {
                    MouseState = MouseState.HOVER;
                    _rippleAM.SecondaryIncrement = 0.08;
                }

                if (ReadOnly) CheckState = _oldCheckState;
            };

            MouseMove += (sender, args) =>
            {
                MouseLocation = args.Location;
                Cursor = IsMouseInCheckArea() ? Cursors.Hand : Cursors.Default;
            };
        }
        #endregion

        #region Private events and methods
        private Bitmap CreateCheckMarkBitmap()
        {
            Bitmap checkMark = new Bitmap(CHECKBOX_SIZE, CHECKBOX_SIZE);
            using (Graphics g = Graphics.FromImage(checkMark))
            {
                // Clear background to transparent
                g.Clear(Color.Transparent);

                // Draw checkmark lines
                using (Pen pen = new Pen(Parent.BackColor, 2))
                {
                    g.DrawLines(pen, CheckmarkLine);
                }
            }

            return checkMark;
        }

        private bool IsMouseInCheckArea()
        {
            return ClientRectangle.Contains(MouseLocation);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (_checkMarkBitmap != null)
                {
                    _checkMarkBitmap.Dispose();
                    _checkMarkBitmap = null;
                }
            }

            base.Dispose(disposing);
        }
        #endregion
    }
}