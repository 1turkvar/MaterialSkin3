using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MaterialSkin
{
    /// <summary>
    /// Fare tekerleği olaylarını, fare imleci üzerinde olan ancak odaklanmamış kontrollere yönlendiren bir sınıf.
    /// </summary>
    public class MouseWheelRedirector : IMessageFilter
    {
        private static MouseWheelRedirector instance = null;
        private static bool _active = false;

        /// <summary>
        /// MouseWheelRedirector'ün aktif olup olmadığını ayarlar veya alır.
        /// </summary>
        public static bool Active
        {
            set
            {
                if (_active != value)
                {
                    _active = value;
                    if (_active)
                    {
                        // Instance'ın null olup olmadığını kontrol et, null ise oluştur
                        if (instance == null)
                            instance = new MouseWheelRedirector();

                        Application.AddMessageFilter(instance);
                    }
                    else if (instance != null)
                    {
                        Application.RemoveMessageFilter(instance);
                    }
                }
            }
            get
            {
                return _active;
            }
        }

        /// <summary>
        /// Belirtilen kontrole MouseWheelRedirector'ü bağlar.
        /// </summary>
        /// <param name="control">Fare tekerleği olaylarını almak için bağlanacak kontrol.</param>
        public static void Attach(Control control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (!_active)
                Active = true;

            control.MouseEnter += instance.ControlMouseEnter;
            control.MouseLeave += instance.ControlMouseLeaveOrDisposed;
            control.Disposed += instance.ControlMouseLeaveOrDisposed;
        }

        /// <summary>
        /// Belirtilen kontrolden MouseWheelRedirector'ü ayırır.
        /// </summary>
        /// <param name="control">Fare tekerleği olaylarının dinlenmesini sonlandırmak için ayrılacak kontrol.</param>
        public static void Detach(Control control)
        {
            if (instance == null || control == null)
                return;

            control.MouseEnter -= instance.ControlMouseEnter;
            control.MouseLeave -= instance.ControlMouseLeaveOrDisposed;
            control.Disposed -= instance.ControlMouseLeaveOrDisposed;

            if (instance.currentControl == control)
                instance.currentControl = null;
        }

        /// <summary>
        /// MouseWheelRedirector sınıfının bir örneğini oluşturur.
        /// </summary>
        private MouseWheelRedirector()
        {
            // Private constructor to enforce singleton pattern
        }

        private Control currentControl;

        private void ControlMouseEnter(object sender, EventArgs e)
        {
            var control = sender as Control;
            if (control != null && !control.Focused)
                currentControl = control;
            else
                currentControl = null;
        }

        private void ControlMouseLeaveOrDisposed(object sender, EventArgs e)
        {
            if (currentControl == sender)
                currentControl = null;
        }

        // Windows mesaj sabitleri
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E; // Yatay fare tekerleği desteği için

        /// <summary>
        /// Uygulama mesajlarını filtreler ve fare tekerleği mesajlarını yönlendirir.
        /// </summary>
        public bool PreFilterMessage(ref Message m)
        {
            if (currentControl != null && (m.Msg == WM_MOUSEWHEEL || m.Msg == WM_MOUSEHWHEEL))
            {
                if (!currentControl.IsDisposed && currentControl.IsHandleCreated)
                {
                    SendMessage(currentControl.Handle, m.Msg, m.WParam, m.LParam);
                    return true; // Mesajın işlendiğini belirt
                }
            }
            return false; // Mesajı normal şekilde işle
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}