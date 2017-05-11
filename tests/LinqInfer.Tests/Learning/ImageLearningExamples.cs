using LinqInfer.Data;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class ImageLearningExamples : TestFixtureBase
    {
        private const int VectorWidth = 7;

        [Test]
        [Category("BuildOmit")] // TODO: System.ArgumentException Invalid or corrupted data on dotnet core linux?
        public void LoadSerialisedNetworkFromXml_ClassifiesAsExpected()
        {
            using (var res = GetResource("net-X-O-I.xml"))
            {
                var xml = XDocument.Load(res);

                var doc = new BinaryVectorDocument(xml, false, BinaryVectorDocument.XmlVectorSerialisationMode.Default);

                var testSet1 = new[] { 'X', 'O', 'Z', 'I' }
                    .Select(c => ToCharObj(c, FontFamily.GenericSansSerif, "testing"))
                    .Select(l => l.ClassifyAs(l.Character))
                    .ToArray();

                var classifier = doc.OpenAsMultilayerNetworkClassifier<Letter, char>(x => x.VectorData, VectorWidth * VectorWidth);

                var result1 = classifier.Classify(testSet1[3].ObjectInstance);

                Assert.That(result1.First().ClassType, Is.EqualTo('I'));
                Assert.That(result1.First().Score, IsAround(0.9, 0.1d));
            }
        }
        
        [TestCase("X,O,I")]
        public void TrainNetwork_UsingCharacterBitmaps_PCATransform(string testChars)
        {
            var letters = testChars.Split(',').Select(c => c[0]).ToArray();

            var randomTrainingSet = FontFamily
                .Families
                .Where(f => f.IsStyleAvailable(FontStyle.Regular) && f != FontFamily.GenericSansSerif && f != FontFamily.GenericSerif)
                .RandomOrder()
                .Take(15)
                .SelectMany(f => letters.Select(c => ToCharObj(c, f, "training")))
                .RandomOrder()
                .ToList()
                .AsQueryable();

            var testSet1 = letters
                .Select(c => ToCharObj(c, FontFamily.GenericSerif, "testing"))
                .RandomOrder()
                .Select(l => l.ClassifyAs(l.Character))
                .ToArray();

            var testSet2 = letters
                .Select(c => ToCharObj(c, FontFamily.GenericSansSerif, "testing"))
                .RandomOrder()
                .Select(l => l.ClassifyAs(l.Character))
                .ToArray();

            var pipeline = randomTrainingSet
                .CreatePipeline(m => m.VectorData, VectorWidth * VectorWidth);

            Console.WriteLine(pipeline.FeatureExtractor.VectorSize);

            var reduced = pipeline.PrincipalComponentReduction(15);

            Console.WriteLine(pipeline.FeatureExtractor.VectorSize);

            int i = 0;

            var fitnessFunction = MultilayerNetworkFitnessFunctions.ClassificationAccuracyFunction(testSet1);

            var classifier = pipeline.ToMultilayerNetworkClassifier(c => c.Character, 0.35f, fitnessFunction)
                .ExecuteUntil(c =>
                {
                    i++;

                    var score = MultilayerNetworkFitnessFunctions.ClassificationAccuracyPercentage(c, testSet1);

                    return i > 50 || score == 1;
                });

            reduced.ToCsv(Console.Out, x => x.Character.ToString()).Execute();

            var doc = classifier.ToVectorDocument();

            int failures = 0;

            foreach (var m in testSet2)
            {
                Console.WriteLine("Classifying {0}", m.Classification);

                var results = classifier.Classify(m.ObjectInstance).ToList();

                if (results.First().ClassType != m.Classification)
                {
                    failures++;
                }

                var distOfOther = results.ToDistribution();

                foreach (var d in distOfOther)
                    Console.WriteLine("{0} = {1}", d.Key, d.Value.ToPercent());
            }

            var data = ((IExportableAsVectorDocument)classifier).ToVectorDocument();

            // data.ExportAsXml().Save(@"..\linqinfer\tests\LinqInfer.Tests\Learning\net-" + testChars.Replace(',', '-') + ".xml");

            Console.WriteLine("{0} = {1}/{2} failed", testChars, failures, letters.Length);
        }

        [TestCase("X,O,I")]
        [TestCase("A,U,T")]
        [TestCase("M,N,U")]
        public void TrainNetwork_UsingCharacterBitmaps(string testChars)
        {
            var letters = testChars.Split(',').Select(c => c[0]).ToArray();

            var randomTrainingSet = FontFamily
                .Families
                .Where(f => f.IsStyleAvailable(FontStyle.Regular) && f != FontFamily.GenericSansSerif && f != FontFamily.GenericSerif)
                .RandomOrder()
                .Take(15)
                .SelectMany(f => letters.Select(c => ToCharObj(c, f, "training")))
                .RandomOrder()
                .ToList()
                .AsQueryable();

            var testSet1 = letters
                .Select(c => ToCharObj(c, FontFamily.GenericSerif, "testing"))
                .RandomOrder()
                .Select(l => l.ClassifyAs(l.Character))
                .ToArray();

            var testSet2 = letters
                .Select(c => ToCharObj(c, FontFamily.GenericSansSerif, "testing"))
                .RandomOrder()
                .Select(l => l.ClassifyAs(l.Character))
                .ToArray();

            var pipeline = randomTrainingSet.CreatePipeline(m => m.VectorData, VectorWidth * VectorWidth);

            Console.WriteLine(pipeline.FeatureExtractor.VectorSize);

            pipeline.ReduceFeaturesByThreshold(0.2f);

            Console.WriteLine(pipeline.FeatureExtractor.VectorSize);

            int i = 0;

            var fitnessFunction = MultilayerNetworkFitnessFunctions.ClassificationAccuracyFunction(testSet1);

            var classifier = pipeline.ToMultilayerNetworkClassifier(c => c.Character, 0.35f, fitnessFunction)
                .ExecuteUntil(c =>
                {
                    i++;

                    var score = MultilayerNetworkFitnessFunctions.ClassificationAccuracyPercentage(c, testSet1);

                    return i > 10 || score == 1;
                });

            int failures = 0;

            foreach (var m in testSet2)
            {
                Console.WriteLine("Classifying {0}", m.Classification);

                var results = classifier.Classify(m.ObjectInstance).ToList();

                if (results.First().ClassType != m.Classification)
                {
                    failures++;
                }

                var distOfOther = results.ToDistribution();

                foreach (var d in distOfOther)
                    Console.WriteLine("other {0} = {1}", d.Key, d.Value.ToPercent());
            }

            Console.WriteLine("{0} = {1}/{2} failures", testChars, failures, letters.Length);
        }

        public Letter ToCharObj(int charVal)
        {
            var c = ((char)(byte)charVal);

            return ToCharObj(c);
        }

        public Letter ToCharObj(char c, FontFamily font = null, string message = null)
        {
            // Console.WriteLine("{0} {1}", font.Name, message);

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

        [DebuggerDisplay("{Character}")]
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
