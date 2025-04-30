namespace MaterialSkin
{
    using System.Drawing;
    using System.Drawing.Drawing2D;

    /// <summary>
    /// Grafik çizim işlemleri için yardımcı araçlar sağlar.
    /// </summary>
    internal static class DrawHelper
    {
        /// <summary>
        /// Belirtilen koordinat ve boyutlarda yuvarlak köşeli dikdörtgen oluşturur.
        /// </summary>
        /// <param name="x">Sol üst köşenin X koordinatı</param>
        /// <param name="y">Sol üst köşenin Y koordinatı</param>
        /// <param name="width">Genişlik</param>
        /// <param name="height">Yükseklik</param>
        /// <param name="radius">Köşe yarıçapı</param>
        /// <returns>Yuvarlak köşeli dikdörtgen şeklinde grafik yolu</returns>
        public static GraphicsPath CreateRoundRect(float x, float y, float width, float height, float radius)
        {
            var gp = new GraphicsPath();
            gp.AddArc(x + width - (radius * 2), y, radius * 2, radius * 2, 270, 90);
            gp.AddArc(x + width - (radius * 2), y + height - (radius * 2), radius * 2, radius * 2, 0, 90);
            gp.AddArc(x, y + height - (radius * 2), radius * 2, radius * 2, 90, 90);
            gp.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            gp.CloseFigure();
            return gp;
        }

        /// <summary>
        /// Belirtilen dikdörtgen ve yarıçap kullanarak yuvarlak köşeli bir dikdörtgen oluşturur.
        /// </summary>
        /// <param name="rect">Dikdörtgen</param>
        /// <param name="radius">Köşe yarıçapı</param>
        /// <returns>Yuvarlak köşeli dikdörtgen şeklinde grafik yolu</returns>
        public static GraphicsPath CreateRoundRect(Rectangle rect, float radius)
        {
            return CreateRoundRect(rect.X, rect.Y, rect.Width, rect.Height, radius);
        }

        /// <summary>
        /// Belirtilen kayan noktalı dikdörtgen ve yarıçap kullanarak yuvarlak köşeli bir dikdörtgen oluşturur.
        /// </summary>
        /// <param name="rect">Kayan noktalı dikdörtgen</param>
        /// <param name="radius">Köşe yarıçapı</param>
        /// <returns>Yuvarlak köşeli dikdörtgen şeklinde grafik yolu</returns>
        public static GraphicsPath CreateRoundRect(RectangleF rect, float radius)
        {
            return CreateRoundRect(rect.X, rect.Y, rect.Width, rect.Height, radius);
        }

        /// <summary>
        /// İki rengi belirtilen oranda karıştırır.
        /// </summary>
        /// <param name="backgroundColor">Arka plan rengi</param>
        /// <param name="frontColor">Ön plan rengi</param>
        /// <param name="blend">Karıştırma oranı (0-255)</param>
        /// <returns>Karıştırılmış renk</returns>
        public static Color BlendColor(Color backgroundColor, Color frontColor, double blend)
        {
            var ratio = blend / 255d;
            var invRatio = 1d - ratio;
            var r = (int)((backgroundColor.R * invRatio) + (frontColor.R * ratio));
            var g = (int)((backgroundColor.G * invRatio) + (frontColor.G * ratio));
            var b = (int)((backgroundColor.B * invRatio) + (frontColor.B * ratio));
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// İki rengi ön plan renginin alfa değerine göre karıştırır.
        /// </summary>
        /// <param name="backgroundColor">Arka plan rengi</param>
        /// <param name="frontColor">Ön plan rengi</param>
        /// <returns>Karıştırılmış renk</returns>
        public static Color BlendColor(Color backgroundColor, Color frontColor)
        {
            return BlendColor(backgroundColor, frontColor, frontColor.A);
        }

        /// <summary>
        /// Kare şeklinde bir gölge çizer.
        /// </summary>
        /// <param name="g">Grafik nesnesi</param>
        /// <param name="bounds">Gölge sınırları</param>
        public static void DrawSquareShadow(Graphics g, Rectangle bounds)
        {
            using (var shadowBrush = new SolidBrush(Color.FromArgb(12, 0, 0, 0)))
            {
                RectangleF[] shadowRects = new RectangleF[]
                {
                    new RectangleF(bounds.X - 3.5f, bounds.Y - 1.5f, bounds.Width + 6, bounds.Height + 6),
                    new RectangleF(bounds.X - 2.5f, bounds.Y - 1.5f, bounds.Width + 4, bounds.Height + 4),
                    new RectangleF(bounds.X - 1.5f, bounds.Y - 0.5f, bounds.Width + 2, bounds.Height + 2),
                    new RectangleF(bounds.X - 0.5f, bounds.Y + 1.5f, bounds.Width, bounds.Height),
                    new RectangleF(bounds.X - 0.5f, bounds.Y + 2.5f, bounds.Width, bounds.Height)
                };

                float[] radiuses = new float[] { 8, 6, 4, 4, 4 };

                for (int i = 0; i < shadowRects.Length; i++)
                {
                    using (var path = CreateRoundRect(shadowRects[i], radiuses[i]))
                    {
                        g.FillPath(shadowBrush, path);
                    }
                }
            }
        }

        /// <summary>
        /// Yuvarlak şeklinde bir gölge çizer.
        /// </summary>
        /// <param name="g">Grafik nesnesi</param>
        /// <param name="bounds">Gölge sınırları</param>
        public static void DrawRoundShadow(Graphics g, Rectangle bounds)
        {
            using (var shadowBrush = new SolidBrush(Color.FromArgb(12, 0, 0, 0)))
            {
                Rectangle[] shadowEllipses = new Rectangle[]
                {
                    new Rectangle(bounds.X - 2, bounds.Y - 1, bounds.Width + 4, bounds.Height + 6),
                    new Rectangle(bounds.X - 1, bounds.Y - 1, bounds.Width + 2, bounds.Height + 4),
                    new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height + 2),
                    new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height),
                    new Rectangle(bounds.X, bounds.Y + 1, bounds.Width, bounds.Height)
                };

                foreach (var ellipse in shadowEllipses)
                {
                    g.FillEllipse(shadowBrush, ellipse);
                }
            }
        }
    }
}