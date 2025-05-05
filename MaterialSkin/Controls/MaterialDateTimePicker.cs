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

        [Category("Material Skin")]
        [DefaultValue(false)]
        public new bool ShowUpDown { get; set; } = false;

        private string _customFormatString = string.Empty;

        /// <summary>
        /// Fires when a date is picked in the calendar dropdown
        /// </summary>
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
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                UpdateColors();
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                         ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.SupportsTransparentBackColor, true);
            }

            public void UpdateColors()
            {
                try
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating MonthCalendar colors: {ex.Message}");
                }
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
        private bool _isDisposing = false;

        private const int WM_REFLECT = 0x2000;
        private const int WM_NOTIFY = 0x004E;
        private const int DTN_FIRST = -760;
        private const int DTN_DROPDOWN = DTN_FIRST - 6;
        private const int DTN_CLOSEUP = DTN_FIRST - 7;

        public MaterialDateTimePicker()
        {
            Format = DateTimePickerFormat.Short;
            Height = 36;
            MinimumSize = new Size(100, 36);

            try
            {
                Font = SkinManager.getFontByType(MaterialSkinManager.fontType.Subtitle1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting font: {ex.Message}");
                Font = new Font("Segoe UI", 10f);
            }

            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            InitializeCustomDropdown();
        }

        /// <summary>
        /// Overriding this property to properly handle format changes
        /// </summary>
        public new DateTimePickerFormat Format
        {
            get { return base.Format; }
            set
            {
                base.Format = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Overriding this property to properly handle custom format changes
        /// </summary>
        public new string CustomFormat
        {
            get { return base.CustomFormat; }
            set
            {
                base.CustomFormat = value;
                _customFormatString = value; // Yedek saklıyoruz olası sorunlar için
                Invalidate();
            }
        }

        private void InitializeCustomDropdown()
        {
            try
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

                _calendar.DateSelected += Calendar_DateSelected;
                _dropdownForm.Deactivate += DropdownForm_Deactivate;
                _dropdownForm.FormClosing += DropdownForm_FormClosing;
                _dropdownForm.Paint += DropdownForm_Paint;

                _dropdownForm.Controls.Add(_calendar);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing dropdown: {ex.Message}");
            }
        }

        private void Calendar_DateSelected(object sender, DateRangeEventArgs e)
        {
            try
            {
                Value = _calendar.SelectionStart;
                DatePicked?.Invoke(this, _calendar.SelectionStart);
                CloseDropdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in calendar date selection: {ex.Message}");
            }
        }

        private void DropdownForm_Deactivate(object sender, EventArgs e)
        {
            CloseDropdown();
        }

        private void DropdownForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                CloseDropdown();
            }
        }

        private void DropdownForm_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(SkinManager.DividersColor))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, _dropdownForm.Width - 1, _dropdownForm.Height - 1);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if ((m.Msg == (WM_REFLECT + WM_NOTIFY)))
            {
                try
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing window message: {ex.Message}");
                }
            }

            base.WndProc(ref m);
        }

        private void ShowCustomDropdown()
        {
            if (_isDropDownOpen || _isDisposing || ShowUpDown) return;

            try
            {
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
            catch (Exception ex)
            {
                _isDropDownOpen = false;
                System.Diagnostics.Debug.WriteLine($"Error showing dropdown: {ex.Message}");
            }
        }

        private void AutoCloseDropdownIfLostFocus(object sender, EventArgs e)
        {
            if (_isDisposing) return;

            try
            {
                if (!_calendar.Focused && !_dropdownForm.Focused && !Focused &&
                    !_dropdownForm.IsDisposed && !_calendar.IsDisposed)
                {
                    Application.Idle -= AutoCloseDropdownIfLostFocus;
                    CloseDropdown();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AutoCloseDropdownIfLostFocus: {ex.Message}");
                Application.Idle -= AutoCloseDropdownIfLostFocus;
            }
        }

        private void CloseDropdown()
        {
            if (_isDisposing) return;

            if (_isDropDownOpen)
            {
                try
                {
                    _isDropDownOpen = false;

                    if (!_dropdownForm.IsDisposed)
                        _dropdownForm.Hide();

                    Application.Idle -= AutoCloseDropdownIfLostFocus;

                    if (!IsDisposed && !_isDisposing)
                        Focus();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error closing dropdown: {ex.Message}");
                    _isDropDownOpen = false;
                    Application.Idle -= AutoCloseDropdownIfLostFocus;
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // Minimum boyut kontrolü
            if (Width < MinimumSize.Width || Height < MinimumSize.Height)
            {
                Size = new Size(
                    Math.Max(Width, MinimumSize.Width),
                    Math.Max(Height, MinimumSize.Height)
                );
            }

            // Dropdown açıksa konumunu güncelle
            if (_isDropDownOpen && !_isDisposing)
            {
                try
                {
                    CloseDropdown();
                    ShowCustomDropdown();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling resize: {ex.Message}");
                }
            }

            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            try
            {
                // ShowUpDown modunda yukarı/aşağı tuşlarını özel olarak işle
                if (ShowUpDown)
                {
                    if (e.KeyCode == Keys.Up)
                    {
                        Value = Value.AddDays(1);
                        e.Handled = true;
                        return;
                    }
                    else if (e.KeyCode == Keys.Down)
                    {
                        Value = Value.AddDays(-1);
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    // Normal mod - dropdown aç/kapat
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling key press: {ex.Message}");
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_isDisposing) return;

            try
            {
                Graphics g = e.Graphics;

                // Arka plan rengini ayarla
                Color parentBackColor = Parent?.BackColor ?? SystemColors.Control;
                g.Clear(parentBackColor);

                Color backgroundColor = Enabled ? SkinManager.BackgroundColor : SkinManager.BackgroundDisabledColor;

                using (SolidBrush backBrush = new SolidBrush(backgroundColor))
                {
                    g.FillRectangle(backBrush, ClientRectangle);
                }

                // Text rengi ve çizimi
                Color textColor = Enabled
                    ? (UseAccentColor ? SkinManager.ColorScheme.AccentColor : SkinManager.TextHighEmphasisColor)
                    : SkinManager.TextDisabledOrHintColor;

                using (SolidBrush textBrush = new SolidBrush(textColor))
                {
                    // Format kontrol ediliyor
                    string displayText = Text;

                    // Custom format durumunda metin işleniyor
                    if (Format == DateTimePickerFormat.Custom && !string.IsNullOrEmpty(CustomFormat))
                    {
                        try
                        {
                            displayText = Value.ToString(CustomFormat);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error formatting date: {ex.Message}");

                            // Yedekten formatlamayı dene
                            if (!string.IsNullOrEmpty(_customFormatString))
                            {
                                try
                                {
                                    displayText = Value.ToString(_customFormatString);
                                }
                                catch
                                {
                                    // Son çare olarak standart formatı kullan
                                    displayText = Value.ToShortDateString();
                                }
                            }
                        }
                    }

                    // Metin çizimi için kırpma alanı belirleyelim (ok için alan bırakarak)
                    int arrowSpace = ShowUpDown ? 25 : 20;
                    Rectangle textRect = new Rectangle(8, 0, Width - arrowSpace - 16, Height);

                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Near;
                        sf.LineAlignment = StringAlignment.Center;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        sf.Trimming = StringTrimming.EllipsisCharacter;

                        g.DrawString(displayText, Font, textBrush, textRect, sf);
                    }
                }

                // Ok ikonu çizimi
                int arrowSize = Math.Min(Height - 16, 20);
                Rectangle arrowRect = new Rectangle(Width - arrowSize - 8, (Height - arrowSize) / 2, arrowSize, arrowSize);

                using (SolidBrush arrowBrush = new SolidBrush(textColor))
                {
                    if (ShowUpDown)
                    {
                        // Up-Down butonları çizimi
                        int halfHeight = arrowRect.Height / 2;

                        // Yukarı ok
                        Point[] upTriangle = new Point[]
                        {
                            new Point(arrowRect.Left + arrowRect.Width / 2 - 4, arrowRect.Top + halfHeight - 2),
                            new Point(arrowRect.Left + arrowRect.Width / 2 + 4, arrowRect.Top + halfHeight - 2),
                            new Point(arrowRect.Left + arrowRect.Width / 2, arrowRect.Top + 2),
                        };
                        g.FillPolygon(arrowBrush, upTriangle);

                        // Aşağı ok
                        Point[] downTriangle = new Point[]
                        {
                            new Point(arrowRect.Left + arrowRect.Width / 2 - 4, arrowRect.Top + halfHeight + 2),
                            new Point(arrowRect.Left + arrowRect.Width / 2 + 4, arrowRect.Top + halfHeight + 2),
                            new Point(arrowRect.Left + arrowRect.Width / 2, arrowRect.Top + arrowRect.Height - 2),
                        };
                        g.FillPolygon(arrowBrush, downTriangle);
                    }
                    else
                    {
                        // Normal aşağı ok
                        Point[] triangle = new Point[]
                        {
                            new Point(arrowRect.Left + arrowRect.Width / 2 - 4, arrowRect.Top + arrowRect.Height / 2 - 2),
                            new Point(arrowRect.Left + arrowRect.Width / 2 + 4, arrowRect.Top + arrowRect.Height / 2 - 2),
                            new Point(arrowRect.Left + arrowRect.Width / 2, arrowRect.Top + arrowRect.Height / 2 + 3),
                        };
                        g.FillPolygon(arrowBrush, triangle);
                    }
                }

                // Kenarlık çizimi
                using (Pen borderPen = new Pen(SkinManager.DividersColor))
                {
                    g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Paint: {ex.Message}");
            }
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            base.OnValueChanged(eventargs);
            Invalidate();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _calendar?.UpdateColors();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposing)
            {
                _isDisposing = true;

                try
                {
                    Application.Idle -= AutoCloseDropdownIfLostFocus;

                    _isDropDownOpen = false;

                    if (_calendar != null)
                    {
                        _calendar.DateSelected -= Calendar_DateSelected;
                        _calendar.Dispose();
                        _calendar = null;
                    }

                    if (_dropdownForm != null)
                    {
                        _dropdownForm.Deactivate -= DropdownForm_Deactivate;
                        _dropdownForm.FormClosing -= DropdownForm_FormClosing;
                        _dropdownForm.Paint -= DropdownForm_Paint;
                        _dropdownForm.Dispose();
                        _dropdownForm = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in Dispose: {ex.Message}");
                }
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