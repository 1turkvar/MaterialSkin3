namespace MaterialSkin.Controls
{
    using MaterialSkin.Animations;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class MaterialSnackBar : MaterialForm
    {
        private const int TOP_PADDING_SINGLE_LINE = 6;
        private const int LEFT_RIGHT_PADDING = 16;
        private const int BUTTON_PADDING = 8;
        private const int BUTTON_HEIGHT = 36;

        private readonly MaterialButton _actionButton = new MaterialButton();
        private readonly Timer _duration = new Timer();      // Timer that checks when the drop down is fully visible

        private readonly AnimationManager _animationManager;
        private bool _closingAnimationDone = false;
        private bool _useAccentColor;
        private bool _closeAnimation = false;

        #region "Events"

        [Category("Action")]
        [Description("Fires when Action button is clicked")]
        public event EventHandler ActionButtonClick;

        #endregion


        [Category("Material Skin"), DefaultValue(false), DisplayName("Use Accent Color")]
        public bool UseAccentColor
        {
            get { return _useAccentColor; }
            set { _useAccentColor = value; UpdateActionButtonAccentColor(); Invalidate(); }
        }


        /// <summary>
        /// Get or Set SnackBar show duration in milliseconds
        /// </summary>
        [Category("Material Skin"), DefaultValue(2000)]
        public int Duration
        {
            get
            {
                return _duration.Interval;
            }
            set
            {
                _duration.Interval = value;
            }
        }

        private string _text;
        /// <summary>
        /// The Text which gets displayed as the Content
        /// </summary>
        [Category("Material Skin"), DefaultValue("SnackBar text")]
        public new string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                UpdateRects();
                Invalidate();
            }
        }

        private bool _showActionButton;
        [Category("Material Skin"), DefaultValue(false), DisplayName("Show Action Button")]
        public bool ShowActionButton
        {
            get { return _showActionButton; }
            set { _showActionButton = value; UpdateRects(); Invalidate(); }
        }

        private string _actionButtonText;
        /// <summary>
        /// The Text which gets displayed as the Content
        /// </summary>
        [Category("Material Skin"), DefaultValue("OK")]
        public string ActionButtonText
        {
            get
            {
                return _actionButtonText;
            }
            set
            {
                _actionButtonText = value;
                if (_actionButton != null)
                {
                    _actionButton.Text = value;
                    UpdateRects();
                }
                Invalidate();
            }
        }

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // width of ellipse
            int nHeightEllipse // height of ellipse
        );

        /// <summary>
        /// Constructor Setting up the Layout
        /// </summary>
        public MaterialSnackBar(string text, int duration, bool showActionButton, string actionButtonText, bool useAccentColor)
        {
            _text = text.Length > 64 ? text.Substring(0, 61) + "..." : text; // 61 karakterden uzun ise kes
            Duration = duration;
            TopMost = true;
            ShowInTaskbar = false;
            Sizable = false;

            BackColor = SkinManager.SnackBarBackgroundColor;
            FormStyle = FormStyles.StatusAndActionBar_None;

            _actionButtonText = actionButtonText;
            _useAccentColor = useAccentColor;
            Height = 48;
            MinimumSize = new Size(600, 48);
            MaximumSize = new Size(600, 48);

            _showActionButton = showActionButton;

            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 6, 6));

            _animationManager = new AnimationManager();
            _animationManager.AnimationType = AnimationType.EaseOut;
            _animationManager.Increment = 0.03;
            _animationManager.OnAnimationProgress += _animationManager_OnAnimationProgress;

            _duration.Tick += Duration_Tick;

            ConfigureActionButton();
            Controls.Add(_actionButton);

            UpdateRects();
        }

        private void ConfigureActionButton()
        {
            _actionButton.AutoSize = false;
            _actionButton.NoAccentTextColor = SkinManager.SnackBarTextButtonNoAccentTextColor;
            _actionButton.DrawShadows = false;
            _actionButton.Type = MaterialButton.MaterialButtonType.Text;
            _actionButton.UseAccentColor = _useAccentColor;
            _actionButton.Visible = _showActionButton;
            _actionButton.Text = _actionButtonText;
            _actionButton.Click += ActionButton_Click;
        }

        private void ActionButton_Click(object sender, EventArgs e)
        {
            ActionButtonClick?.Invoke(this, new EventArgs());
            _closingAnimationDone = false;
            CleanupResources();
            Close();
        }

        private void UpdateActionButtonAccentColor()
        {
            if (_actionButton != null)
            {
                _actionButton.UseAccentColor = _useAccentColor;
            }
        }

        public MaterialSnackBar() : this("SnackBar Text", 3000, false, "OK", false)
        {
        }

        public MaterialSnackBar(string text) : this(text, 3000, false, "OK", false)
        {
        }

        public MaterialSnackBar(string text, int duration) : this(text, duration, false, "OK", false)
        {
        }

        public MaterialSnackBar(string text, string actionButtonText) : this(text, 3000, true, actionButtonText, false)
        {
        }

        public MaterialSnackBar(string text, string actionButtonText, bool useAccentColor) : this(text, 3000, true, actionButtonText, useAccentColor)
        {
        }

        public MaterialSnackBar(string text, int duration, string actionButtonText) : this(text, duration, true, actionButtonText, false)
        {
        }

        public MaterialSnackBar(string text, int duration, string actionButtonText, bool useAccentColor) : this(text, duration, true, actionButtonText, useAccentColor)
        {
        }

        private void UpdateRects()
        {
            // Buton genişliğini hesapla
            int buttonWidth = 0;
            if (_showActionButton)
            {
                buttonWidth = TextRenderer.MeasureText(ActionButtonText, SkinManager.getFontByType(MaterialSkinManager.fontType.Button)).Width + 32;
            }

            // Kullanılabilir maksimum metin genişliği
            int availableTextWidth = MaximumSize.Width - (2 * LEFT_RIGHT_PADDING) - (buttonWidth > 0 ? buttonWidth + BUTTON_PADDING : 0);

            // Metin boyutunu hesapla
            Size textSize = TextRenderer.MeasureText(_text, SkinManager.getFontByType(MaterialSkinManager.fontType.Body2),
                new Size(availableTextWidth, 0), TextFormatFlags.WordBreak);

            // Yeni genişlik ve yükseklik hesapla
            int newWidth = Math.Min(MaximumSize.Width,
                            textSize.Width + (2 * LEFT_RIGHT_PADDING) + (buttonWidth > 0 ? buttonWidth + BUTTON_PADDING : 0));
            newWidth = Math.Max(MinimumSize.Width, newWidth);

            // Metin yüksekliğine göre form yüksekliğini güncelle (minimum 48px)
            int newHeight = Math.Max(48, textSize.Height + (2 * TOP_PADDING_SINGLE_LINE));

            // Boyutları güncelle
            if (Width != newWidth || Height != newHeight)
            {
                Size = new Size(newWidth, newHeight);
            }

            // Buton konumunu güncelle
            if (_showActionButton)
            {
                _actionButton.Width = buttonWidth;
                _actionButton.Height = BUTTON_HEIGHT;
                _actionButton.Text = _actionButtonText;
                // Buton dikey ortalanmış olsun
                _actionButton.Top = (Height - BUTTON_HEIGHT) / 2;
                _actionButton.Left = Width - BUTTON_PADDING - buttonWidth;
                _actionButton.UseAccentColor = _useAccentColor;
            }
            else
            {
                _actionButton.Width = 0;
                _actionButton.Left = Width;
            }
            _actionButton.Visible = _showActionButton;

            // Köşeleri yuvarlak yap
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 6, 6));
        }

        private void Duration_Tick(object sender, EventArgs e)
        {
            _duration.Stop();
            _closingAnimationDone = false;
            CleanupResources();
            Close();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRects();
        }

        /// <summary>
        /// Sets up the Starting Location and starts the Animation
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Owner != null)
            {
                Location = new Point(Convert.ToInt32(Owner.Location.X + (Owner.Width / 2) - (Width / 2)), Convert.ToInt32(Owner.Location.Y + Owner.Height - 60));
                _animationManager.StartNewAnimation(AnimationDirection.In);
                _duration.Start();
            }
        }

        /// <summary>
        /// Animates the Form slides
        /// </summary>
        void _animationManager_OnAnimationProgress(object sender)
        {
            if (_closeAnimation)
            {
                Opacity = _animationManager.GetProgress();
            }
        }

        /// <summary>
        /// Overrides the Paint to create the solid colored backcolor
        /// </summary>
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            e.Graphics.Clear(BackColor);

            // Calc text Rect
            Rectangle textRect = new Rectangle(
                LEFT_RIGHT_PADDING,
                0,
                Width - (2 * LEFT_RIGHT_PADDING) - (_showActionButton ? _actionButton.Width : 0),
                Height);

            // Draw Text
            using (NativeTextRenderer nativeText = new NativeTextRenderer(g))
            {
                // Draw header text
                nativeText.DrawTransparentText(
                    _text,
                    SkinManager.getLogFontByType(MaterialSkinManager.fontType.Body2),
                    SkinManager.SnackBarTextHighEmphasisColor,
                    textRect.Location,
                    textRect.Size,
                    NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
            }
        }

        /// <summary>
        /// Overrides the Closing Event to Animate the Slide Out
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !_closingAnimationDone;
            if (!_closingAnimationDone)
            {
                _closeAnimation = true;
                _animationManager.Increment = 0.06;
                _animationManager.OnAnimationFinished += _animationManager_OnAnimationFinished;
                _animationManager.StartNewAnimation(AnimationDirection.Out);
            }
            base.OnClosing(e);
        }

        /// <summary>
        /// Closes the Form after the pull out animation
        /// </summary>
        void _animationManager_OnAnimationFinished(object sender)
        {
            _closingAnimationDone = true;
            CleanupResources();
            Close();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            _closingAnimationDone = false;
            CleanupResources();
            Close();
        }

        /// <summary>
        /// Cleans up resources and unsubscribes from events
        /// </summary>
        private void CleanupResources()
        {
            _animationManager.OnAnimationFinished -= _animationManager_OnAnimationFinished;
        }

        /// <summary>
        /// Forces the SnackBar to close immediately bypassing animations
        /// </summary>
        public void ForceClose()
        {
            _closingAnimationDone = true;
            CleanupResources();
            Close();
        }

        /// <summary>
        /// Prevents the Form from being dragged
        /// </summary>
        protected override void WndProc(ref Message message)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MOVE = 0xF010;

            switch (message.Msg)
            {
                case WM_SYSCOMMAND:
                    int command = message.WParam.ToInt32() & 0xfff0;
                    if (command == SC_MOVE)
                        return;
                    break;
            }

            base.WndProc(ref message);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _actionButton.Click -= ActionButton_Click;
                _animationManager.OnAnimationProgress -= _animationManager_OnAnimationProgress;
                _duration.Tick -= Duration_Tick;
                _duration.Dispose();
            }
            base.Dispose(disposing);
        }

        public new void Show()
        {
            if (Owner == null)
            {
                throw new Exception("Owner is null. Set Owner first.");
            }
            base.Show();
        }
    }
}