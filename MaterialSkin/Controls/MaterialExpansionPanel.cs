using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace MaterialSkin.Controls
{
    /// <summary>
    /// A Material Design inspired expansion panel control
    /// </summary>
    public class MaterialExpansionPanel : Panel, IMaterialControl
    {
        #region "Private members"

        private MaterialButton _validationButton;
        private MaterialButton _cancelButton;

        private const int _expansionPanelDefaultPadding = 16;
        private const int _leftrightPadding = 24;
        private const int _buttonPadding = 8;
        private const int _expandcollapsbuttonsize = 24;
        private const int _textHeaderHeight = 24;
        private const int _headerHeightCollapse = 48;
        private const int _headerHeightExpand = 64;
        private const int _footerHeight = 68;
        private const int _footerButtonHeight = 36;
        private const int _minHeight = 200;
        private int _headerHeight;

        private bool _collapse;
        private bool _useAccentColor;
        private int _expandHeight;

        private string _titleHeader;
        private string _descriptionHeader;
        private string _validationButtonText;
        private string _cancelButtonText;

        private bool _showValidationButtons;
        private bool _showCollapseExpand;
        private bool _drawShadows;
        private bool _shadowDrawEventSubscribed = false;
        private Rectangle _headerBounds;
        private Rectangle _expandcollapseBounds;
        private Rectangle _savebuttonBounds;
        private Rectangle _cancelbuttonBounds;
        private bool _savebuttonEnable;

        private Control _oldParent;

        private enum ButtonState
        {
            SaveOver,
            CancelOver,
            ColapseExpandOver,
            HeaderOver,
            None
        }

        private ButtonState _buttonState = ButtonState.None;

        #endregion

        #region "Public Properties"

        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        [Category("Material Skin"), DefaultValue(false), DisplayName("Use Accent Color")]
        public bool UseAccentColor
        {
            get => _useAccentColor;
            set
            {
                _useAccentColor = value;
                if (_validationButton != null)
                    _validationButton.UseAccentColor = value;
                if (_cancelButton != null)
                    _cancelButton.UseAccentColor = value;

                UpdateRects();
                Invalidate();
            }
        }

        [DefaultValue(false)]
        [Description("Collapses the control when set to true")]
        [Category("Material Skin")]
        public bool Collapse
        {
            get => _collapse;
            set
            {
                if (_collapse != value)
                {
                    CancelEventArgs e = new CancelEventArgs();
                    if (value)
                        OnBeforeCollapse(e);
                    else
                        OnBeforeExpand(e);

                    if (!e.Cancel)
                    {
                        _collapse = value;
                        CollapseOrExpand();
                        Invalidate();
                    }
                }
            }
        }

        [DefaultValue("Title")]
        [Category("Material Skin"), DisplayName("Title")]
        [Description("Title to show in expansion panel's header")]
        public string Title
        {
            get => _titleHeader;
            set
            {
                _titleHeader = value;
                Invalidate();
            }
        }

        [DefaultValue("Description")]
        [Category("Material Skin"), DisplayName("Description")]
        [Description("Description to show in expansion panel's header")]
        public string Description
        {
            get => _descriptionHeader;
            set
            {
                _descriptionHeader = value;
                Invalidate();
            }
        }

        [DefaultValue(true)]
        [Category("Material Skin"), DisplayName("Draw Shadows")]
        [Description("Draw Shadows around control")]
        public bool DrawShadows
        {
            get => _drawShadows;
            set
            {
                _drawShadows = value;
                if (Parent != null)
                {
                    if (value)
                        AddShadowPaintEvent(Parent, DrawShadowOnParent);
                    else
                        RemoveShadowPaintEvent(Parent, DrawShadowOnParent);
                }
                Invalidate();
            }
        }

        [DefaultValue(240)]
        [Category("Material Skin"), DisplayName("Expand Height")]
        [Description("Define control height when expanded")]
        public int ExpandHeight
        {
            get => _expandHeight;
            set
            {
                _expandHeight = Math.Max(value, _minHeight);
                if (!_collapse && Height != _expandHeight)
                    Height = _expandHeight;
                Invalidate();
            }
        }

        [DefaultValue(true)]
        [Category("Material Skin"), DisplayName("Show collapse/expand")]
        [Description("Show collapse/expand indicator")]
        public bool ShowCollapseExpand
        {
            get => _showCollapseExpand;
            set
            {
                _showCollapseExpand = value;
                Invalidate();
            }
        }

        [DefaultValue(true)]
        [Category("Material Skin"), DisplayName("Show validation buttons")]
        [Description("Show save/cancel button")]
        public bool ShowValidationButtons
        {
            get => _showValidationButtons;
            set
            {
                _showValidationButtons = value;
                if (_validationButton != null)
                    _validationButton.Visible = value;
                if (_cancelButton != null)
                    _cancelButton.Visible = value;

                UpdateRects();
                Invalidate();
            }
        }

        [DefaultValue("SAVE")]
        [Category("Material Skin"), DisplayName("Validation button text")]
        [Description("Set Validation button text")]
        public string ValidationButtonText
        {
            get => _validationButtonText;
            set
            {
                _validationButtonText = value;
                if (_validationButton != null)
                    _validationButton.Text = value;

                UpdateRects();
                Invalidate();
            }
        }

        [DefaultValue("CANCEL")]
        [Category("Material Skin"), DisplayName("Cancel button text")]
        [Description("Set Cancel button text")]
        public string CancelButtonText
        {
            get => _cancelButtonText;
            set
            {
                _cancelButtonText = value;
                if (_cancelButton != null)
                    _cancelButton.Text = value;

                UpdateRects();
                Invalidate();
            }
        }

        [DefaultValue(false)]
        [Category("Material Skin"), DisplayName("Validation button enable")]
        [Description("Enable validation button")]
        public bool ValidationButtonEnable
        {
            get => _savebuttonEnable;
            set
            {
                _savebuttonEnable = value;
                if (_validationButton != null)
                    _validationButton.Enabled = value;

                UpdateRects();
                Invalidate();
            }
        }

        #endregion

        #region "Events"

        /// <summary>
        /// Fires before the panel is collapsed, allowing cancellation
        /// </summary>
        [Category("Disposition")]
        [Description("Fires before the panel is collapsed")]
        public event CancelEventHandler BeforeCollapse;

        /// <summary>
        /// Fires before the panel is expanded, allowing cancellation
        /// </summary>
        [Category("Disposition")]
        [Description("Fires before the panel is expanded")]
        public event CancelEventHandler BeforeExpand;

        /// <summary>
        /// Fires when Save button is clicked
        /// </summary>
        [Category("Action")]
        [Description("Fires when Save button is clicked")]
        public event EventHandler SaveClick;

        /// <summary>
        /// Fires when Cancel button is clicked
        /// </summary>
        [Category("Action")]
        [Description("Fires when Cancel button is clicked")]
        public event EventHandler CancelClick;

        /// <summary>
        /// Fires when Panel is collapsed
        /// </summary>
        [Category("Disposition")]
        [Description("Fires when Panel is collapsed")]
        public event EventHandler PanelCollapse;

        /// <summary>
        /// Fires when Panel is expanded
        /// </summary>
        [Category("Disposition")]
        [Description("Fires when Panel is expanded")]
        public event EventHandler PanelExpand;

        #endregion

        #region "Constructors and Initialization"

        public MaterialExpansionPanel()
        {
            ShowValidationButtons = true;
            ValidationButtonEnable = false;
            ValidationButtonText = "SAVE";
            CancelButtonText = "CANCEL";
            ShowCollapseExpand = true;
            Collapse = false;
            Title = "Title";
            Description = "Description";
            DrawShadows = true;
            ExpandHeight = 240;
            AutoScroll = false;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = SkinManager.BackgroundColor;
            ForeColor = SkinManager.TextHighEmphasisColor;

            Padding = new Padding(24, 64, 24, 16);
            Margin = new Padding(3, 16, 3, 16);
            Size = new Size(480, ExpandHeight);
            _headerHeight = _headerHeightExpand;

            InitializeButtons();
            UpdateRects();
        }

        private void InitializeButtons()
        {
            _validationButton = new MaterialButton
            {
                DrawShadows = false,
                Type = MaterialButton.MaterialButtonType.Text,
                UseAccentColor = _useAccentColor,
                Enabled = ValidationButtonEnable,
                Visible = _showValidationButtons,
                Text = ValidationButtonText
            };
            _cancelButton = new MaterialButton
            {
                DrawShadows = false,
                Type = MaterialButton.MaterialButtonType.Text,
                UseAccentColor = _useAccentColor,
                Visible = _showValidationButtons,
                Text = CancelButtonText
            };

            if (!Controls.Contains(_validationButton))
            {
                Controls.Add(_validationButton);
            }
            if (!Controls.Contains(_cancelButton))
            {
                Controls.Add(_cancelButton);
            }

            _validationButton.Click += _validationButton_Click;
            _cancelButton.Click += _cancelButton_Click;
        }

        #endregion

        #region "Event Handlers"

        private void _cancelButton_Click(object sender, EventArgs e)
        {
            CancelClick?.Invoke(this, new EventArgs());
            Collapse = true;
        }

        private void _validationButton_Click(object sender, EventArgs e)
        {
            SaveClick?.Invoke(this, new EventArgs());
            Collapse = true;
        }

        protected virtual void OnBeforeCollapse(CancelEventArgs e)
        {
            BeforeCollapse?.Invoke(this, e);
        }

        protected virtual void OnBeforeExpand(CancelEventArgs e)
        {
            BeforeExpand?.Invoke(this, e);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            Font = SkinManager.getFontByType(MaterialSkinManager.fontType.Body1);
        }

        protected override void InitLayout()
        {
            base.InitLayout();
            LocationChanged += (sender, e) => { Parent?.Invalidate(); };
            ForeColor = SkinManager.TextHighEmphasisColor;
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (Parent != null && _drawShadows)
                AddShadowPaintEvent(Parent, DrawShadowOnParent);
            if (_oldParent != null)
                RemoveShadowPaintEvent(_oldParent, DrawShadowOnParent);
            _oldParent = Parent;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Parent == null) return;
            if (Visible && _drawShadows)
                AddShadowPaintEvent(Parent, DrawShadowOnParent);
            else
                RemoveShadowPaintEvent(Parent, DrawShadowOnParent);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // Clean up event handlers when the control is destroyed
            if (_oldParent != null)
                RemoveShadowPaintEvent(_oldParent, DrawShadowOnParent);

            base.OnHandleDestroyed(e);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            BackColor = SkinManager.BackgroundColor;
        }

        protected override void OnResize(EventArgs e)
        {
            if (!_collapse)
            {
                if (DesignMode)
                {
                    _expandHeight = Height;
                }
                if (Height < _minHeight) Height = _minHeight;
            }
            else
            {
                Height = _headerHeightCollapse;
            }

            base.OnResize(e);

            _headerBounds = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, _headerHeight);
            _expandcollapseBounds = new Rectangle(
                (Width) - _leftrightPadding - _expandcollapsbuttonsize,
                (int)((_headerHeight - _expandcollapsbuttonsize) / 2),
                _expandcollapsbuttonsize,
                _expandcollapsbuttonsize);

            UpdateRects();

            if (Parent != null && _drawShadows)
            {
                RemoveShadowPaintEvent(Parent, DrawShadowOnParent);
                AddShadowPaintEvent(Parent, DrawShadowOnParent);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (DesignMode)
                return;

            var oldState = _buttonState;

            if (_savebuttonBounds.Contains(e.Location))
                _buttonState = ButtonState.SaveOver;
            else if (_cancelbuttonBounds.Contains(e.Location))
                _buttonState = ButtonState.CancelOver;
            else if (_expandcollapseBounds.Contains(e.Location))
            {
                Cursor = Cursors.Hand;
                _buttonState = ButtonState.ColapseExpandOver;
            }
            else if (_headerBounds.Contains(e.Location))
            {
                Cursor = Cursors.Hand;
                _buttonState = ButtonState.HeaderOver;
            }
            else
            {
                Cursor = Cursors.Default;
                _buttonState = ButtonState.None;
            }

            if (oldState != _buttonState) Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Enabled && (_buttonState == ButtonState.HeaderOver || _buttonState == ButtonState.ColapseExpandOver))
            {
                Collapse = !Collapse;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (DesignMode)
                return;

            Cursor = Cursors.Arrow;
            _buttonState = ButtonState.None;
            Invalidate();
        }

        #endregion

        #region "Drawing Methods"

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            g.Clear(Parent.BackColor);

            // Card rectangle path
            RectangleF expansionPanelRectF = new RectangleF(ClientRectangle.Location, ClientRectangle.Size);
            expansionPanelRectF.X -= 0.5f;
            expansionPanelRectF.Y -= 0.5f;
            GraphicsPath expansionPanelPath = DrawHelper.CreateRoundRect(expansionPanelRectF, 2);

            // Button shadow (blend with form shadow)
            if (_drawShadows)
                DrawHelper.DrawSquareShadow(g, ClientRectangle);

            // Draw expansion panel
            if (!Enabled)
            {
                // Disabled state
                using (SolidBrush disabledBrush = new SolidBrush(
                    DrawHelper.BlendColor(Parent.BackColor, SkinManager.BackgroundDisabledColor, SkinManager.BackgroundDisabledColor.A)))
                {
                    g.FillPath(disabledBrush, expansionPanelPath);
                }
            }
            else
            {
                // Normal state
                if ((_buttonState == ButtonState.HeaderOver || _buttonState == ButtonState.ColapseExpandOver) && _collapse)
                {
                    // Hover state on collapsed panel
                    RectangleF expansionPanelBorderRectF = new RectangleF(
                        ClientRectangle.X + 1, ClientRectangle.Y + 1,
                        ClientRectangle.Width - 2, ClientRectangle.Height - 2);
                    expansionPanelBorderRectF.X -= 0.5f;
                    expansionPanelBorderRectF.Y -= 0.5f;
                    GraphicsPath expansionPanelBoarderPath = DrawHelper.CreateRoundRect(expansionPanelBorderRectF, 2);

                    g.FillPath(SkinManager.ExpansionPanelFocusBrush, expansionPanelBoarderPath);
                }
                else
                {
                    // Normal state
                    using (SolidBrush normalBrush = new SolidBrush(SkinManager.BackgroundColor))
                    {
                        g.FillPath(normalBrush, expansionPanelPath);
                    }
                }
            }

            // Calculate text rectangle
            Rectangle headerRect = new Rectangle(
                _leftrightPadding,
                (_headerHeight - _textHeaderHeight) / 2,
                TextRenderer.MeasureText(_titleHeader, Font).Width + _expansionPanelDefaultPadding,
                _textHeaderHeight);

            // Draw headers
            using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
            {
                // Draw header text
                NativeText.DrawTransparentText(
                    _titleHeader,
                    SkinManager.getLogFontByType(MaterialSkinManager.fontType.Body1),
                    Enabled ? SkinManager.TextHighEmphasisColor : SkinManager.TextDisabledOrHintColor,
                    headerRect.Location,
                    headerRect.Size,
                    NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
            }

            if (!String.IsNullOrEmpty(_descriptionHeader))
            {
                // Draw description header text 
                Rectangle headerDescriptionRect = new Rectangle(
                    headerRect.Right + _expansionPanelDefaultPadding,
                    (_headerHeight - _textHeaderHeight) / 2,
                    _expandcollapseBounds.Left - (headerRect.Right + _expansionPanelDefaultPadding) - _expansionPanelDefaultPadding,
                    _textHeaderHeight);

                using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
                {
                    NativeText.DrawTransparentText(
                        _descriptionHeader,
                        SkinManager.getLogFontByType(MaterialSkinManager.fontType.Body1),
                        SkinManager.TextDisabledOrHintColor,
                        headerDescriptionRect.Location,
                        headerDescriptionRect.Size,
                        NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                }
            }

            if (_showCollapseExpand)
            {
                using (var formButtonsPen = new Pen(
                    _useAccentColor && Enabled ? SkinManager.ColorScheme.AccentColor : SkinManager.TextDisabledOrHintColor, 2))
                {
                    if (_collapse)
                    {
                        // Draw Expand button
                        GraphicsPath pth = new GraphicsPath();
                        PointF TopLeft = new PointF(_expandcollapseBounds.X + 6, _expandcollapseBounds.Y + 9);
                        PointF MidBottom = new PointF(_expandcollapseBounds.X + 12, _expandcollapseBounds.Y + 15);
                        PointF TopRight = new PointF(_expandcollapseBounds.X + 18, _expandcollapseBounds.Y + 9);
                        pth.AddLine(TopLeft, MidBottom);
                        pth.AddLine(TopRight, MidBottom);
                        g.DrawPath(formButtonsPen, pth);
                    }
                    else
                    {
                        // Draw Collapse button
                        GraphicsPath pth = new GraphicsPath();
                        PointF BottomLeft = new PointF(_expandcollapseBounds.X + 6, _expandcollapseBounds.Y + 15);
                        PointF MidTop = new PointF(_expandcollapseBounds.X + 12, _expandcollapseBounds.Y + 9);
                        PointF BottomRight = new PointF(_expandcollapseBounds.X + 18, _expandcollapseBounds.Y + 15);
                        pth.AddLine(BottomLeft, MidTop);
                        pth.AddLine(BottomRight, MidTop);
                        g.DrawPath(formButtonsPen, pth);
                    }
                }
            }

            if (!_collapse && _showValidationButtons)
            {
                // Draw divider
                g.DrawLine(
                    new Pen(SkinManager.DividersColor, 1),
                    new Point(0, Height - _footerHeight),
                    new Point(Width, Height - _footerHeight));
            }
        }

        private void DrawShadowOnParent(object sender, PaintEventArgs e)
        {
            if (Parent == null)
            {
                RemoveShadowPaintEvent((Control)sender, DrawShadowOnParent);
                return;
            }

            if (!_drawShadows || Parent == null) return;

            // Paint shadow on parent
            Graphics gp = e.Graphics;
            Rectangle rect = new Rectangle(Location, ClientRectangle.Size);
            gp.SmoothingMode = SmoothingMode.AntiAlias;
            DrawHelper.DrawSquareShadow(gp, rect);
        }

        #endregion

        #region "Helper Methods"

        private void CollapseOrExpand()
        {
            if (_collapse)
            {
                _headerHeight = _headerHeightCollapse;
                Height = _headerHeightCollapse;
                Margin = new Padding(16, 1, 16, 0);

                PanelCollapse?.Invoke(this, new EventArgs());
            }
            else
            {
                _headerHeight = _headerHeightExpand;
                Height = _expandHeight;
                Margin = new Padding(16, 16, 16, 16);

                PanelExpand?.Invoke(this, new EventArgs());
            }

            Refresh();
        }

        private void UpdateRects()
        {
            if (!_collapse && _showValidationButtons)
            {
                int buttonWidth = ((TextRenderer.MeasureText(ValidationButtonText,
                    SkinManager.getFontByType(MaterialSkinManager.fontType.Button))).Width + 32);

                _savebuttonBounds = new Rectangle(
                    (Width) - _buttonPadding - buttonWidth,
                    Height - _expansionPanelDefaultPadding - _footerButtonHeight,
                    buttonWidth,
                    _footerButtonHeight);

                buttonWidth = ((TextRenderer.MeasureText(CancelButtonText,
                    SkinManager.getFontByType(MaterialSkinManager.fontType.Button))).Width + 32);

                _cancelbuttonBounds = new Rectangle(
                    _savebuttonBounds.Left - _buttonPadding - buttonWidth,
                    Height - _expansionPanelDefaultPadding - _footerButtonHeight,
                    buttonWidth,
                    _footerButtonHeight);

                if (_validationButton != null)
                {
                    _validationButton.Width = _savebuttonBounds.Width;
                    _validationButton.Left = Width - _buttonPadding - _validationButton.Width;
                    _validationButton.Top = _savebuttonBounds.Top;
                    _validationButton.Height = _savebuttonBounds.Height;
                    _validationButton.Text = _validationButtonText;
                    _validationButton.Enabled = _savebuttonEnable;
                    _validationButton.UseAccentColor = _useAccentColor;
                    _validationButton.Visible = _showValidationButtons;
                }

                if (_cancelButton != null)
                {
                    _cancelButton.Width = _cancelbuttonBounds.Width;
                    _cancelButton.Left = _validationButton.Left - _buttonPadding - _cancelbuttonBounds.Width;
                    _cancelButton.Top = _cancelbuttonBounds.Top;
                    _cancelButton.Height = _cancelbuttonBounds.Height;
                    _cancelButton.Text = _cancelButtonText;
                    _cancelButton.UseAccentColor = _useAccentColor;
                    _cancelButton.Visible = _showValidationButtons;
                }
            }
        }

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

        #endregion
    }
}