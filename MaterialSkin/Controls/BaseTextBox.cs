namespace MaterialSkin.Controls
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Base class for Material Design TextBox control
    /// </summary>
    [ToolboxItem(false)]
    public class BaseTextBox : TextBox, IMaterialControl
    {
        #region "Public Properties"

        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        [Browsable(false)]
        public int Depth { get; set; }

        /// <summary>
        /// Gets the skin manager instance.
        /// </summary>
        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        /// <summary>
        /// Gets or sets the mouse state.
        /// </summary>
        [Browsable(false)]
        public MouseState MouseState { get; set; }

        private string hint = string.Empty;
        /// <summary>
        /// Gets or sets the hint text displayed when the control is empty.
        /// </summary>
        public string Hint
        {
            get { return hint; }
            set
            {
                hint = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Selects all text in the control.
        /// </summary>
        public new void SelectAll()
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                base.Focus();
                base.SelectAll();
            });
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTextBox"/> class.
        /// </summary>
        public BaseTextBox()
        {
        }

        /// <summary>
        /// Raises the GotFocus event.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        /// <summary>
        /// Raises the LostFocus event.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        private const int WM_ENABLE = 0x0A;
        private const int WM_PAINT = 0xF;
        private const uint WM_USER = 0x0400;
        private const uint EM_SETBKGNDCOLOR = (WM_USER + 67);
        private const uint WM_KILLFOCUS = 0x0008;

        /// <summary>
        /// Processes Windows messages.
        /// </summary>
        /// <param name="m">The Windows Message to process.</param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_ENABLE)
            {
                using (Graphics g = Graphics.FromHwnd(Handle))
                {
                    if (g != null)
                    {
                        Rectangle bounds = new Rectangle(0, 0, Width, Height);
                        g.FillRectangle(SkinManager.BackgroundDisabledBrush, bounds);
                    }
                }
            }

            if (m.Msg == WM_PAINT && string.IsNullOrEmpty(Text) && !Focused)
            {
                using (Graphics graphics = Graphics.FromHwnd(m.HWnd))
                {
                    if (graphics != null)
                    {
                        using (NativeTextRenderer NativeText = new NativeTextRenderer(graphics))
                        {
                            NativeText.DrawTransparentText(
                                Hint,
                                SkinManager.getFontByType(MaterialSkinManager.fontType.Subtitle1),
                                Enabled ?
                                    ColorHelper.RemoveAlpha(SkinManager.TextMediumEmphasisColor, BackColor) : // not focused
                                    ColorHelper.RemoveAlpha(SkinManager.TextDisabledOrHintColor, BackColor), // Disabled
                                ClientRectangle.Location,
                                ClientRectangle.Size,
                                NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Top);
                        }
                    }
                }
            }

            if (m.Msg == EM_SETBKGNDCOLOR)
            {
                Invalidate();
            }

            if (m.Msg == WM_KILLFOCUS) //set border back to normal on lost focus
            {
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Base class for Material Design MaskedTextBox control
    /// </summary>
    [ToolboxItem(false)]
    public class BaseMaskedTextBox : MaskedTextBox, IMaterialControl
    {
        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        [Browsable(false)]
        public int Depth { get; set; }

        /// <summary>
        /// Gets the skin manager instance.
        /// </summary>
        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        /// <summary>
        /// Gets or sets the mouse state.
        /// </summary>
        [Browsable(false)]
        public MouseState MouseState { get; set; }

        private string hint = string.Empty;
        /// <summary>
        /// Gets or sets the hint text displayed when the control is empty.
        /// </summary>
        public string Hint
        {
            get { return hint; }
            set
            {
                hint = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Selects all text in the control.
        /// </summary>
        public new void SelectAll()
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                base.Focus();
                base.SelectAll();
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMaskedTextBox"/> class.
        /// </summary>
        public BaseMaskedTextBox()
        {
        }

        /// <summary>
        /// Raises the GotFocus event.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        /// <summary>
        /// Raises the LostFocus event.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        private const int WM_ENABLE = 0x0A;
        private const int WM_PAINT = 0xF;
        private const uint WM_USER = 0x0400;
        private const uint EM_SETBKGNDCOLOR = (WM_USER + 67);
        private const uint WM_KILLFOCUS = 0x0008;

        /// <summary>
        /// Processes Windows messages.
        /// </summary>
        /// <param name="m">The Windows Message to process.</param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_ENABLE)
            {
                using (Graphics g = Graphics.FromHwnd(Handle))
                {
                    if (g != null)
                    {
                        Rectangle bounds = new Rectangle(0, 0, Width, Height);
                        g.FillRectangle(SkinManager.BackgroundDisabledBrush, bounds);
                    }
                }
            }

            if (m.Msg == WM_PAINT && string.IsNullOrEmpty(Text) && !Focused)
            {
                using (Graphics graphics = Graphics.FromHwnd(m.HWnd))
                {
                    if (graphics != null)
                    {
                        using (NativeTextRenderer NativeText = new NativeTextRenderer(graphics))
                        {
                            NativeText.DrawTransparentText(
                                Hint,
                                SkinManager.getFontByType(MaterialSkinManager.fontType.Subtitle1),
                                Enabled ?
                                    ColorHelper.RemoveAlpha(SkinManager.TextMediumEmphasisColor, BackColor) : // not focused
                                    ColorHelper.RemoveAlpha(SkinManager.TextDisabledOrHintColor, BackColor), // Disabled
                                ClientRectangle.Location,
                                ClientRectangle.Size,
                                NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Top);
                        }
                    }
                }
            }

            if (m.Msg == EM_SETBKGNDCOLOR)
            {
                Invalidate();
            }

            if (m.Msg == WM_KILLFOCUS) //set border back to normal on lost focus
            {
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Context menu strip for BaseTextBox control
    /// </summary>
    [ToolboxItem(false)]
    public class BaseTextBoxContextMenuStrip : MaterialContextMenuStrip
    {
        /// <summary>
        /// Undo menu item
        /// </summary>
        public readonly ToolStripItem undo = new MaterialToolStripMenuItem { Text = "Undo" };

        /// <summary>
        /// First separator
        /// </summary>
        public readonly ToolStripItem seperator1 = new ToolStripSeparator();

        /// <summary>
        /// Cut menu item
        /// </summary>
        public readonly ToolStripItem cut = new MaterialToolStripMenuItem { Text = "Cut" };

        /// <summary>
        /// Copy menu item
        /// </summary>
        public readonly ToolStripItem copy = new MaterialToolStripMenuItem { Text = "Copy" };

        /// <summary>
        /// Paste menu item
        /// </summary>
        public readonly ToolStripItem paste = new MaterialToolStripMenuItem { Text = "Paste" };

        /// <summary>
        /// Delete menu item
        /// </summary>
        public readonly ToolStripItem delete = new MaterialToolStripMenuItem { Text = "Delete" };

        /// <summary>
        /// Second separator
        /// </summary>
        public readonly ToolStripItem seperator2 = new ToolStripSeparator();

        /// <summary>
        /// Select All menu item
        /// </summary>
        public readonly ToolStripItem selectAll = new MaterialToolStripMenuItem { Text = "Select All" };

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTextBoxContextMenuStrip"/> class.
        /// </summary>
        public BaseTextBoxContextMenuStrip()
        {
            Items.AddRange(new[]
            {
                undo,
                seperator1,
                cut,
                copy,
                paste,
                delete,
                seperator2,
                selectAll
            });
        }
    }
}