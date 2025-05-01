namespace MaterialSkin.Controls
{
    using MaterialSkin;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class MaterialDateTimePicker : DateTimePicker, IMaterialControl
    {
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        [Category("Material Skin")]
        public bool UseAccentColor { get; set; } = false;

        public event EventHandler<DateTime> DatePicked;

        private class MaterialMonthCalendar : MonthCalendar
        {
            private readonly MaterialDateTimePicker _owner;
            private Color _backgroundColor;
            private Color _titleBackColor;
            private Color _titleForeColor;
            private Color _foreColor;
            private Color _trailingForeColor;

            public MaterialMonthCalendar(MaterialDateTimePicker owner)
            {
                _owner = owner;
                UpdateColors();
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                         ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.SupportsTransparentBackColor, true);
            }

            public void UpdateColors()
            {
                _backgroundColor = _owner.SkinManager.BackgroundColor;
                _titleBackColor = _owner.UseAccentColor ?
                    _owner.SkinManager.ColorScheme.AccentColor :
                    _owner.SkinManager.ColorScheme.PrimaryColor;
                _titleForeColor = _owner.SkinManager.TextHighEmphasisColor;
                _foreColor = _owner.SkinManager.TextHighEmphasisColor;
                _trailingForeColor = _owner.SkinManager.TextDisabledOrHintColor;

                BackColor = _backgroundColor;
                ForeColor = _foreColor;
                TitleBackColor = _titleBackColor;
                TitleForeColor = _titleForeColor;
                TrailingForeColor = _trailingForeColor;

                ShowTodayCircle = true;
                CalendarDimensions = new Size(1, 1);
                if (BoldedDates.Length == 0 || BoldedDates[0] != DateTime.Today)
                    BoldedDates = new DateTime[] { DateTime.Today };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                using (Pen p = new Pen(_owner.SkinManager.DividersColor))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
                }
            }
        }

        private Form _dropdownForm;
        private MaterialMonthCalendar _calendar;
        private bool _isDropDownOpen = false;

        private const int WM_REFLECT = 0x2000;
        private const int WM_NOTIFY = 0x004E;
        private const int DTN_FIRST = -760;
        private const int DTN_DROPDOWN = DTN_FIRST - 6;
        private const int DTN_CLOSEUP = DTN_FIRST - 7;

        public MaterialDateTimePicker()
        {
            Format = DateTimePickerFormat.Short;
            Height = 36;
            Font = SkinManager.getFontByType(MaterialSkinManager.fontType.Subtitle1);

            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            InitializeCustomDropdown();
        }

        private void InitializeCustomDropdown()
        {
            _dropdownForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                BackColor = SkinManager.BackgroundColor,
                TopMost = true
            };

            _calendar = new MaterialMonthCalendar(this)
            {
                Dock = DockStyle.None,
                MaxSelectionCount = 1,
                Font = SkinManager.getFontByType(MaterialSkinManager.fontType.Body1),
                Size = new Size(250, 180)
            };

            _dropdownForm.ClientSize = new Size(_calendar.Width + 4, _calendar.Height + 4);
            _calendar.Location = new Point(2, 2);

            _calendar.DateSelected += (sender, e) =>
            {
                Value = _calendar.SelectionStart;
                DatePicked?.Invoke(this, _calendar.SelectionStart);
                CloseDropdown();
            };

            _dropdownForm.Deactivate += (sender, e) => CloseDropdown();
            _dropdownForm.FormClosing += (sender, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    CloseDropdown();
                }
            };

            _dropdownForm.Paint += (sender, e) =>
            {
                using (Pen pen = new Pen(SkinManager.DividersColor))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, _dropdownForm.Width - 1, _dropdownForm.Height - 1);
                }
            };

            _dropdownForm.Controls.Add(_calendar);
        }

        protected override void WndProc(ref Message m)
        {
            if ((m.Msg == (WM_REFLECT + WM_NOTIFY)))
            {
                NMHDR nmhdr = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));

                if (nmhdr.code == DTN_DROPDOWN)
                {
                    ShowCustomDropdown();
                    return;
                }
                else if (nmhdr.code == DTN_CLOSEUP)
                {
                    CloseDropdown();
                }
            }

            base.WndProc(ref m);
        }

        private void ShowCustomDropdown()
        {
            if (_isDropDownOpen) return;

            _isDropDownOpen = true;

            _calendar.SetDate(Value);
            _calendar.SelectionStart = Value;
            _calendar.SelectionEnd = Value;
            _calendar.UpdateColors();

            Point location = PointToScreen(new Point(0, Height));
            Rectangle screenBounds = Screen.FromControl(this).WorkingArea;

            if (location.Y + _dropdownForm.Height > screenBounds.Bottom)
                location.Y = PointToScreen(new Point(0, 0)).Y - _dropdownForm.Height;

            if (location.X + _dropdownForm.Width > screenBounds.Right)
                location.X = screenBounds.Right - _dropdownForm.Width;

            _dropdownForm.Location = location;
            _dropdownForm.Show(this);
            _calendar.Focus();

            // Kapatma kontrolü için Application.Idle içinde kontrol
            Application.Idle += AutoCloseDropdownIfLostFocus;
        }

        private void AutoCloseDropdownIfLostFocus(object sender, EventArgs e)
        {
            if (!_calendar.Focused && !_dropdownForm.Focused && !Focused)
            {
                Application.Idle -= AutoCloseDropdownIfLostFocus;
                CloseDropdown();
            }
        }

        private void CloseDropdown()
        {
            if (_isDropDownOpen)
            {
                _isDropDownOpen = false;
                _dropdownForm.Hide();
                Application.Idle -= AutoCloseDropdownIfLostFocus;
                Focus();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if ((e.KeyCode == Keys.Down || e.KeyCode == Keys.Return) && !_isDropDownOpen)
            {
                ShowCustomDropdown();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && _isDropDownOpen)
            {
                CloseDropdown();
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.Clear(Parent?.BackColor ?? SystemColors.Control);

            Color backgroundColor = Enabled ? SkinManager.BackgroundColor : SkinManager.BackgroundDisabledColor;

            using (SolidBrush backBrush = new SolidBrush(backgroundColor))
            {
                g.FillRectangle(backBrush, ClientRectangle);
            }

            Color textColor = Enabled
                ? (UseAccentColor ? SkinManager.ColorScheme.AccentColor : SkinManager.TextHighEmphasisColor)
                : SkinManager.TextDisabledOrHintColor;

            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(Text, Font, textBrush, new PointF(8, (Height - Font.Height) / 2));
            }

            int arrowSize = Height - 16;
            Rectangle arrowRect = new Rectangle(Width - arrowSize - 8, 8, arrowSize, arrowSize);

            using (SolidBrush arrowBrush = new SolidBrush(textColor))
            {
                Point[] triangle = new Point[]
                {
                    new Point(arrowRect.Left + arrowRect.Width / 2 - 4, arrowRect.Top + arrowRect.Height / 2 - 2),
                    new Point(arrowRect.Left + arrowRect.Width / 2 + 4, arrowRect.Top + arrowRect.Height / 2 - 2),
                    new Point(arrowRect.Left + arrowRect.Width / 2, arrowRect.Top + arrowRect.Height / 2 + 3),
                };
                g.FillPolygon(arrowBrush, triangle);
            }

            using (Pen borderPen = new Pen(SkinManager.DividersColor))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            base.OnValueChanged(eventargs);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _calendar?.Dispose();
                _dropdownForm?.Dispose();
            }
            base.Dispose(disposing);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NMHDR
        {
            public IntPtr hwndFrom;
            public IntPtr idFrom;
            public int code;
        }
    }
}
