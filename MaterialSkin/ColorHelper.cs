using System;
using System.Drawing;

namespace MaterialSkin
{
    public static class ColorHelper
    {
        /// <summary>
        /// Rengi belirtilen yüzde oranında açar.
        /// </summary>
        /// <param name="color">Açılacak renk.</param>
        /// <param name="percent">Açma yüzdesi. Örnek: 0.1 rengi %10 daha açık yapacaktır.</param>
        /// <returns>Açılmış yeni renk.</returns>
        public static Color Lighten(this Color color, float percent)
        {
            if (percent < 0)
            {
                throw new ArgumentOutOfRangeException("percent", "Yüzde değeri negatif olamaz.");
            }

            var lighting = color.GetBrightness();
            lighting = lighting + lighting * percent;

            if (lighting > 1.0)
            {
                lighting = 1.0f;
            }
            else if (lighting <= 0)
            {
                lighting = 0.1f;
            }

            return FromHsl(color.A, color.GetHue(), color.GetSaturation(), lighting);
        }

        /// <summary>
        /// Rengi belirtilen yüzde oranında koyulaştırır.
        /// </summary>
        /// <param name="color">Koyulaştırılacak renk.</param>
        /// <param name="percent">Koyulaştırma yüzdesi. Örnek: 0.1 rengi %10 daha koyu yapacaktır.</param>
        /// <returns>Koyulaştırılmış yeni renk.</returns>
        public static Color Darken(this Color color, float percent)
        {
            if (percent < 0)
            {
                throw new ArgumentOutOfRangeException("percent", "Yüzde değeri negatif olamaz.");
            }

            var lighting = color.GetBrightness();
            lighting = lighting - lighting * percent;

            if (lighting > 1.0)
            {
                lighting = 1.0f;
            }
            else if (lighting <= 0)
            {
                lighting = 0.0f;
            }

            return FromHsl(color.A, color.GetHue(), color.GetSaturation(), lighting);
        }

        /// <summary>
        /// HSL değerlerini bir Color nesnesine dönüştürür.
        /// </summary>
        /// <param name="alpha">Alfa değeri (şeffaflık).</param>
        /// <param name="hue">Renk tonu (0-360).</param>
        /// <param name="saturation">Doygunluk (0-1).</param>
        /// <param name="lighting">Aydınlık (0-1).</param>
        /// <returns>Oluşturulan renk.</returns>
        public static Color FromHsl(int alpha, float hue, float saturation, float lighting)
        {
            if (alpha < 0 || alpha > 255)
            {
                throw new ArgumentOutOfRangeException("alpha", "Alfa değeri 0-255 aralığında olmalıdır.");
            }
            if (hue < 0f || hue > 360f)
            {
                throw new ArgumentOutOfRangeException("hue", "Renk tonu 0-360 aralığında olmalıdır.");
            }
            if (saturation < 0f || saturation > 1f)
            {
                throw new ArgumentOutOfRangeException("saturation", "Doygunluk 0-1 aralığında olmalıdır.");
            }
            if (lighting < 0f || lighting > 1f)
            {
                throw new ArgumentOutOfRangeException("lighting", "Aydınlık 0-1 aralığında olmalıdır.");
            }

            if (saturation == 0)
            {
                int value = Convert.ToInt32(lighting * 255);
                return Color.FromArgb(alpha, value, value, value);
            }

            float fMax, fMid, fMin;
            int iSextant, iMax, iMid, iMin;

            if (lighting > 0.5)
            {
                fMax = lighting - (lighting * saturation) + saturation;
                fMin = lighting + (lighting * saturation) - saturation;
            }
            else
            {
                fMax = lighting + (lighting * saturation);
                fMin = lighting - (lighting * saturation);
            }

            iSextant = (int)Math.Floor(hue / 60f);
            if (hue >= 300f)
            {
                hue -= 360f;
            }
            hue /= 60f;
            hue -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
            if (iSextant % 2 == 0)
            {
                fMid = hue * (fMax - fMin) + fMin;
            }
            else
            {
                fMid = fMin - hue * (fMax - fMin);
            }

            iMax = Convert.ToInt32(fMax * 255);
            iMid = Convert.ToInt32(fMid * 255);
            iMin = Convert.ToInt32(fMin * 255);

            // Değerlerin 0-255 aralığında olduğundan emin olalım
            iMax = Math.Max(0, Math.Min(255, iMax));
            iMid = Math.Max(0, Math.Min(255, iMid));
            iMin = Math.Max(0, Math.Min(255, iMin));

            switch (iSextant)
            {
                case 1:
                    return Color.FromArgb(alpha, iMid, iMax, iMin);
                case 2:
                    return Color.FromArgb(alpha, iMin, iMax, iMid);
                case 3:
                    return Color.FromArgb(alpha, iMin, iMid, iMax);
                case 4:
                    return Color.FromArgb(alpha, iMid, iMin, iMax);
                case 5:
                    return Color.FromArgb(alpha, iMax, iMin, iMid);
                default:
                    return Color.FromArgb(alpha, iMax, iMid, iMin);
            }
        }

        /// <summary>
        /// Alfa değerini kaldırarak renkten saydam olmayan bir renk oluşturur.
        /// </summary>
        /// <param name="foreground">Ön plan rengi.</param>
        /// <param name="background">Arka plan rengi.</param>
        /// <returns>Saydam olmayan birleştirilmiş renk.</returns>
        public static Color RemoveAlpha(Color foreground, Color background)
        {
            if (foreground.A == 255)
                return foreground;

            var alpha = foreground.A / 255.0;
            var diff = 1.0 - alpha;
            return Color.FromArgb(255,
                (byte)(foreground.R * alpha + background.R * diff),
                (byte)(foreground.G * alpha + background.G * diff),
                (byte)(foreground.B * alpha + background.B * diff));
        }
    }
}