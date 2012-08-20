using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace AcoDraw
{
    // Reference Documents:
    // http://web.archive.org/web/20101115171444/http://image-d.isp.jp/commentary/color_cformula/index.html
    // http://w3.kcua.ac.jp/~fujiwara/infosci/colorspace/colorspace3.html

    /// <summary>
    /// Provides methods for converting from a byte-array to <see cref="System.Drawing.Color"/>.
    /// </summary>
    public static class ColorConverter
    {
        /// <summary>
        /// Converts from RGB.
        /// </summary>
        public static Color FromRgb(IEnumerable<byte> source)
        {
            return Color.FromArgb(
                (int)(Utility.ToUInt16(source.Take(2)) / 256),
                (int)(Utility.ToUInt16(source.Skip(2).Take(2)) / 256),
                (int)(Utility.ToUInt16(source.Skip(4).Take(2)) / 256));
        }

        /// <summary>
        /// Converts from HSB.
        /// </summary>
        public static Color FromHsb(IEnumerable<byte> source)
        {
            // http://ja.wikipedia.org/wiki/HSV%E8%89%B2%E7%A9%BA%E9%96%93
            var h = Utility.ToUInt16(source.Take(2)) / 182.04;
            var s = Utility.ToUInt16(source.Skip(2).Take(2)) / 655.35;
            var b = Utility.ToUInt16(source.Skip(4).Take(2)) / 655.35;

            // Convert to RGB
            var h_i = (int)(Math.Floor(h / 60) % 6);
            var f = (h / 60) - h_i;
            var p = (int)(b * (1 - s));
            var q = (int)(b * (1 - f * s));
            var t = (int)(b * (1 - (1 - f) * s));
            switch (h_i)
            {
                case 0:
                    return Color.FromArgb((int)b, t, p);
                case 1:
                    return Color.FromArgb(q, (int)b, p);
                case 2:
                    return Color.FromArgb(p, (int)b, t);
                case 3:
                    return Color.FromArgb(p, q, (int)b);
                case 4:
                    return Color.FromArgb(t, p, (int)b);
                case 5:
                    return Color.FromArgb((int)b, p, q);
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// Converts from CMYK.
        /// </summary>
        public static Color FromCmyk(IEnumerable<byte> source)
        {
            var c = 1 - (Utility.ToUInt16(source.Take(2)) / 65535.0);
            var m = 1 - (Utility.ToUInt16(source.Skip(2).Take(2)) / 65535.0);
            var y = 1 - (Utility.ToUInt16(source.Skip(4).Take(2)) / 65535.0);
            var k = 1 - (Utility.ToUInt16(source.Skip(6).Take(2)) / 65535.0);

            return FromCmyk(c, m, y, k);
        }

        /// <summary>
        /// Converts from L*a*b*.
        /// </summary>
        public static Color FromLab(IEnumerable<byte> source)
        {
            // (6/29)^3 = 0.008856...

            const double Xn = 0.9642;
            const double Yn = 1.0;
            const double Zn = 0.8249;
            var MInv = new[,] {
                {  3.134187, -1.617209, -0.490694 },
                { -0.978749,  1.916130,  0.033433 },
                {  0.071964, -0.228994,  1.405754 }
            };

            // L*: [0 .. 100]
            // a*, b*: [-128 .. 127]
            var L = Utility.ToUInt16(source.Take(2)) / 100.0;
            var a = Utility.ToInt16(source.Skip(2).Take(2)) / 100.0;
            var b = Utility.ToInt16(source.Skip(4).Take(2)) / 100.0;

            // [0 .. 1.0]
            #region L*a*b* -> XYZ
            var f_y = (L + 16) / 116;
            var f_x = f_y + (a / 500);
            var f_z = f_y - (b / 200);

            double Y;
            if (f_y > 6 / 29.0)
                Y = Math.Pow(f_y, 3) * Yn;
            else
                Y = Math.Pow(3 / 29.0, 3) * (116 * f_y - 16) * Yn;

            double X;
            if (f_x > 6 / 29.0)
                X = Math.Pow(f_x, 3) * Xn;
            else
                X = Math.Pow(3 / 29.0, 3) * (116 * f_x - 16) * Xn;

            double Z;
            if (f_z > 6 / 29.0)
                Z = Math.Pow(f_z, 3) * Zn;
            else
                Z = Math.Pow(3 / 29.0, 3) * (116 * f_z - 16) * Zn;

            // D50 -> D65
            var Cs = new[,] {
                { X },
                { Y },
                { Z }
            };
            var transformed = new double[3, 1];
            for (int i = 0; i < MInv.GetLength(0); ++i)
            {
                for (int j = 0; j < Cs.GetLength(1); ++j)
                {
                    transformed[i,j] = 0;
                    for (int k = 0; k < MInv.GetLength(1); ++k)
                    {
                        transformed[i, j] += MInv[i, k] * Cs[k, j];
                    }
                }
            }
            X = transformed[0, 0];
            Y = transformed[1, 0];
            Z = transformed[2, 0];
            #endregion

            #region XYZ -> RGB
            return Color.FromArgb(
                (int)(255 * ( 3.240479 * X - 1.537150 * Y - 0.498535 * Z)),
                (int)(255 * (-0.969256 * X + 1.875991 * Y + 0.041556 * Z)),
                (int)(255 * ( 0.055648 * X - 0.204043 * Y + 1.057331 * Z)));
            #endregion
        }

        /// <summary>
        /// Converts from Grayscale.
        /// </summary>
        public static Color FromGrayscale(IEnumerable<byte> source)
        {
            // value range = [0 .. 10000] --> val / 39.0625 --> [0 .. 256)
            var x = (int)(Utility.ToUInt16(source.Take(2)) / 39.0625);
            return Color.FromArgb(x, x, x);
        }

        /// <summary>
        /// Converts from Wide CMYK.
        /// </summary>
        public static Color FromWideCmyk(IEnumerable<byte> source)
        {
            var c = Utility.ToUInt16(source.Take(2)) / 100;
            var m = Utility.ToUInt16(source.Skip(2).Take(2)) / 100;
            var y = Utility.ToUInt16(source.Skip(4).Take(2)) / 100;
            var k = Utility.ToUInt16(source.Skip(6).Take(2)) / 100;

            return FromCmyk(c, m, y, k);
        }

        /// <summary>
        /// Converts from CMYK.
        /// </summary>
        static Color FromCmyk(double c, double m, double y, double k)
        {
            var r = 1 - Math.Min(1, c * (1 - k) + k);
            var g = 1 - Math.Min(1, m * (1 - k) + k);
            var b = 1 - Math.Min(1, y * (1 - k) + k);

            return Color.FromArgb((int)r * 255, (int)g * 255, (int)b * 255);
        }
    }
}

