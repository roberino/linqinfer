using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using draw = System.Drawing;

namespace LinqInfer.Tests.Learning
{
    public static class ImageSampleGeneration
    {
        public static IEnumerable<char> Characters(char from, char to)
        {
            return Enumerable
                .Range(from, to - from + 1)
                .Select(n => (char)(byte)n);
        }

        public static IEnumerable<char> Characters(params char[] values)
        {
            return values;
        }

        public static IEnumerable<Letter> Letters(this IEnumerable<char> characters, int width, FontFamily font)
        {
            return characters
                .Select(c => ToCharObj(c, new Rectangle() { Width = width, Height = width }, font));
        }

        public static Letter ToCharObj(int charVal)
        {
            var c = ((char)(byte)charVal);

            return ToCharObj(c);
        }

        public static Letter ToCharObj(char c, 
            Rectangle? bounds = null,
            FontFamily font = null)
        {
            // Console.WriteLine("{0} {1}", font.Name, message);

            bounds = bounds ?? new Rectangle(0, 0, 7, 7);

            using (var b = new Bitmap(bounds.Value.Width, bounds.Value.Height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(b))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    g.DrawString(c.ToString(), new Font(font ?? FontFamily.GenericMonospace, bounds.Value.Width, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.Black, new PointF());

                    g.Flush();
                    g.Save();
                }

                var savePath = AppDomain.CurrentDomain.BaseDirectory + "\\" + c;

                b.Save(savePath + ".png", ImageFormat.Png);

                var data = BitmapToDoubleArray(b);

                File.WriteAllText(savePath + ".txt", ArrayToTable(data, bounds.Value.Width));

                //Print(c, font.Name, data);

                return new Letter
                {
                    Character = c,
                    VectorData = new ColumnVector1D(data)
                };
            }
        }

        private static string ArrayToTable(double[] data, int width)
        {
            var builder = new StringBuilder();

            int c = 0;
            int r = 0;

            foreach (var x in data)
            {
                if (c == 0) builder.Append($"{r++}:");

                builder.Append(x > 100 ? 'X' : x > 50 ? '.' : ' ');

                c++;

                if (c == width)
                {
                    c = 0;
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static double[] BitmapToDoubleArray1(Bitmap b)
        {
            var data = new double[b.Width * b.Height];

            foreach (var y in Enumerable.Range(0, b.Height))
            {
                foreach (var x in Enumerable.Range(0, b.Width))
                {
                    var i = (x + y * b.Width);
                    data[i] = GetValue(b.GetPixel(x, y));
                }
            }

            return data;
        }

        private static double[] BitmapToDoubleArray(Bitmap bmp)
        {
            var data = bmp.LockBits(new draw.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

            try
            {
                var vector = new double[bmp.Width * bmp.Height];

                unsafe
                {
                    ColorARGB* startingPosition = (ColorARGB*)data.Scan0;

                    for (int i = 0; i < bmp.Height; i++)
                    {
                        for (int j = 0; j < bmp.Width; j++)
                        {
                            ColorARGB* position = startingPosition + j + i * bmp.Width;

                            vector[i * bmp.Width + j] = GetValue(Color.FromArgb(position->A, position->R, position->G, position->B));
                        }
                    }
                }

                return vector;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        public struct ColorARGB
        {
            public byte B;
            public byte G;
            public byte R;
            public byte A;

            public ColorARGB(Color color)
            {
                A = color.A;
                R = color.R;
                G = color.G;
                B = color.B;
            }

            public ColorARGB(byte a, byte r, byte g, byte b)
            {
                A = a;
                R = r;
                G = g;
                B = b;
            }
        }

        private static byte[] BitmapToBytes(Bitmap bmp)
        {
            var data = bmp.LockBits(new draw.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

            try
            {
                IntPtr ptr = data.Scan0;
                int bytes = Math.Abs(data.Stride) * bmp.Height;
                byte[] rgbValues = new byte[bytes];
                Marshal.Copy(ptr, rgbValues, 0, bytes);

                return rgbValues;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        public static double GetValue(Color colour)
        {
            var blackness = 255 - ((colour.R + colour.G + colour.B) / 3d);

            return blackness * (colour.A / 255d);

            //var blackness = 255 - (colour.R * 0.2126 + colour.G * 0.7152 + colour.B * 0.0722);

            //return (1d / (colour.A + 1)) * blackness;

            //return colour.IsEmpty ? 0 : 1; // (1 - ((colour.R * 0.2126 + colour.G * 0.7152 + colour.B * 0.0722) / 255));
        }

        [DebuggerDisplay("{Character}")]
        public class Letter
        {
            public char Character { get; set; }

            [Feature]
            public IVector VectorData { get; set; }
        }
    }
}
