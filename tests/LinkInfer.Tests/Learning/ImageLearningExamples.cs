using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using LinqInfer.Maths.Probability;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class ImageLearningExamples
    {
        private const int VectorWidth = 10;

        [Test]
        public void TrainNetwork_UsingCharacterBitmaps()
        {
            var x = ToCharObj('X', FontFamily.GenericSansSerif);
            var i = ToCharObj('I', FontFamily.GenericSansSerif);

            var x2 = ToCharObj('X', FontFamily.GenericSerif);
            var i2 = ToCharObj('I', FontFamily.GenericSerif);

            // var letters = Enumerable.Range((int)'A', 26).Select(b => (char)b).ToList();

            var letters = new[] { 'X', 'O', 'I' };

            var randomTrainingSet = FontFamily
                .Families
                .Where(f => f.IsStyleAvailable(FontStyle.Regular) && f != FontFamily.GenericSansSerif && f != FontFamily.GenericSerif)
                .RandomOrder()
                .Take(5)
                .SelectMany(f => letters.Select(c => ToCharObj(c, f)))
                .RandomOrder()
                .ToList()
                .AsQueryable();

            var pipeline = randomTrainingSet.CreatePipeline(m => m == null ? new double[10] : m.VectorData);

            pipeline.PreprocessWith(m =>
            {
                return m.Skip(3).ToArray();
            });

            var classifier = pipeline.ToMultilayerNetworkClassifier(c => c.Character, 0.3f).ExecuteUntil(c =>
            {
                var r = c.Classify(x);
                var r2 = c.Classify(i);

                return r.Any() && r.First().ClassType == 'X' && r2.First().ClassType == 'I';
            });

            var x2c = classifier.Classify(x2).ToHypotheses();

            x2c.Update(v => v == 'X' ? (1).OutOf(2) : (1).OutOf(5));

            Assert.That(classifier.Classify(x).First().ClassType == 'X');
            Assert.That(classifier.Classify(x2).First().ClassType == 'X');

            var classifyResults = classifier.Classify(i).ToList();

            Assert.That(classifyResults.First().ClassType == 'I');
        }

        public Letter ToCharObj(int charVal)
        {
            var c = ((char)(byte)charVal);

            return ToCharObj(c);
        }

        public Letter ToCharObj(char c, FontFamily font = null)
        {
            using (var b = new Bitmap(VectorWidth, VectorWidth))
            {
                using (var g = Graphics.FromImage(b))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    g.DrawString(c.ToString(), new Font(font ?? FontFamily.GenericMonospace, VectorWidth, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.Black, new PointF());

                    g.Flush();
                    g.Save();
                }

                //b.Save(AppDomain.CurrentDomain.BaseDirectory + "\\" + c + ".png", ImageFormat.Png);

                var data = BitmapToDoubleArray(b);

                //Print(c, font.Name, data);

                return new Letter
                {
                    Character = c,
                    VectorData = data
                };
            }
        }

        private void Print(char c, string fontName, double[] data)
        {
            using (var writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\data.txt", true))
            {
                writer.Write(c);
                writer.Write(":\t");

                foreach (var x in data)
                {
                    writer.Write(x);
                    writer.Write('\t');
                }

                writer.WriteLine();
            }
        }

        private double[] BitmapToDoubleArray1(Bitmap b)
        {
            var data = new double[b.Width * b.Height];

            foreach (var x in Enumerable.Range(1, b.Width))
            {
                foreach (var y in Enumerable.Range(1, b.Height))
                {
                    int i = (x * y) - 1;
                    data[i] = GetValue(b.GetPixel(x - 1, y - 1));
                }
            }

            return data;
        }

        private double[] BitmapToDoubleArray(Bitmap bmp)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

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

                            vector[i * j] = GetValue(Color.FromArgb(position->A, position->R, position->G, position->B));
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

        private byte[] BitmapToBytes(Bitmap bmp)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

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

        public double GetValue(Color colour)
        {
            var blackness = 255 - (colour.R * 0.2126 + colour.G * 0.7152 + colour.B * 0.0722);

            return (1d / (colour.A + 1)) * blackness;

            //return colour.IsEmpty ? 0 : 1; // (1 - ((colour.R * 0.2126 + colour.G * 0.7152 + colour.B * 0.0722) / 255));
        }

        public class Letter
        {
            public char Character { get; set; }

            [Feature]
            public double[] VectorData { get; set; }
        }

        public class ByteArrayConverter : IValueConverter
        {
            public bool CanConvert(Type type)
            {
                throw new NotImplementedException();
            }

            public double Convert(object value)
            {
                throw new NotImplementedException();
            }
        }
    }
}
