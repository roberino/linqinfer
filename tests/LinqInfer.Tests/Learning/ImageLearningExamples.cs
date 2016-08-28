using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Maths.Probability;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int VectorWidth = 7;

        [Test]
        [Ignore("WIP")]
        public void TrainNetwork_UsingCharacterBitmaps_ClusterThenEvaluate()
        {
            var x = ToCharObj('X', FontFamily.GenericMonospace);

            var letters = Enumerable.Range((int)'A', 26).Select(b => (char)b).ToList();

            var initialTrainingSet = letters
                .Select(c => ToCharObj(c, FontFamily.GenericMonospace))
                .AsQueryable();

            var s = VectorWidth * VectorWidth;

            var initialPipeline = initialTrainingSet.CreatePipeline(m => m.VectorData, s);

            // initialPipeline.ReduceFeaturesByThreshold(0.2f);

            var clusters = initialPipeline.ToSofm(25, 0.25f, 0.2f).Execute();

            var clusterPipeline = clusters.CreatePipeine();

            var classifier = clusterPipeline.ToMultilayerNetworkClassifier(m => m.Key, 0.45f).Execute();

            var classifiers = clusters
                .Select(c => c.AsQueryable().CreatePipeline(m => m.VectorData, s)
                .ToMultilayerNetworkClassifier(m => m.Character, 0.35f).Execute());

            var xNode = clusters.CreateNewNode(x);

            var xResults = classifier.Classify(xNode).Join(clusters.AsEnumerable(), c => c.ClassType, o => o.Key, (o, c) => new
            {
                node = c,
                cls = o
            }).ToList();

            foreach (var cls in classifiers)
            {
                Console.WriteLine(cls.ToString());
            }

            var xResults1 = classifiers.Select(c => c.Classify(x)).ToList();
            
            foreach(var res in xResults1)
            {
                Console.WriteLine("Cluster:");

                foreach(var c in res)
                {
                    Console.WriteLine("\t {0}= {1}", c.ClassType, c.Score);
                }
            }
        }

        [Test]
        [Ignore("WIP")]
        public void TrainNetwork_UsingCharacterBitmaps_Cluster()
        {
            var x = ToCharObj('X', FontFamily.GenericSansSerif);

            var letters = Enumerable.Range((int)'A', 26).Select(b => (char)b).ToList();

            var initialTrainingSet = letters
                .Select(c => ToCharObj(c, FontFamily.GenericMonospace))
                .AsQueryable();

            var s = VectorWidth * VectorWidth;

            var initialPipeline = initialTrainingSet.CreatePipeline(m => m.VectorData, s);

            initialPipeline.ReduceFeaturesByThreshold(0.2f);

            var clusters = initialPipeline.ToSofm(25, 0.25f, 0.1f).Execute();

            var classifiers = new Dictionary<IPrunableObjectClassifier<char, Letter>, int>();

            foreach (var cluster in clusters.Where(c => c.Count() > 1))
            {
                var testSet = cluster
                    .Select(c => ToCharObj(c.Character, FontFamily.GenericSerif, "testing"))
                    .RandomOrder()
                    .Select(l => l.ClassifyAs(l.Character))
                    .ToArray();

                var randomTrainingSet = FontFamily
                    .Families
                    .Where(f => f.IsStyleAvailable(FontStyle.Regular) && f != FontFamily.GenericSansSerif && f != FontFamily.GenericSerif)
                    .RandomOrder()
                    .Take(15)
                    .SelectMany(f => cluster.Select(c => ToCharObj(c.Character, f, "training")))
                    .RandomOrder()
                    .ToList()
                    .AsQueryable();

                var fitnessFunction = MultilayerNetworkFitnessFunctions.ClassificationAccuracyFunction(testSet);

                var pipelineA = randomTrainingSet.CreatePipeline(c => c.VectorData, s);

                pipelineA.ReduceFeaturesByThreshold(0.2f);

                var nn = pipelineA.ToMultilayerNetworkClassifier(o => o.Character, 0.35f, fitnessFunction).ExecuteUntil(c =>
                {
                    var score = MultilayerNetworkFitnessFunctions.ClassificationAccuracyPercentage(c, testSet);

                    return score >= 0.5f;
                });

                classifiers[nn] = clusters.Count();
            }

            var hypos = classifiers.AsHypotheses();

            var results = hypos.Hypotheses.SelectMany(c => c.Outcome.Classify(x).ToDistribution()).GroupBy(c => c.Key).Select(c => new
            {
                cls = c.Key,
                score = c.Aggregate(1d, (t, v) => v.Value.Value * t)
            })
            .OrderByDescending(c => c.score)
            .ToList();

            hypos.Update(h => h.HighestClassification(x).Item2);

            var result = hypos.MostProbable().Classify(x);
        }

        //[Test]
        public void TrainNetwork_UsingCharacterBitmaps_Adapt()
        {
            var x = ToCharObj('X', FontFamily.GenericSansSerif);

            var letters = Enumerable.Range((int)'A', 26).Select(b => (char)b).ToList();

            var randomTrainingSet =
                new[] { FontFamily.GenericSansSerif, FontFamily.GenericSerif }
                .SelectMany(f => letters.Select(c => ToCharObj(c, f, "training")))
                .RandomOrder()
                .ToList()
                .AsQueryable();

            var testSet = letters
                .Select(c => ToCharObj(c, FontFamily.GenericMonospace, "testing"))
                .RandomOrder()
                .Select(l => l.ClassifyAs(l.Character))
                .ToArray();

            var pipeline = randomTrainingSet.CreatePipeline(m => m == null ? new double[VectorWidth * VectorWidth] : m.VectorData);

            Console.WriteLine(pipeline.FeatureExtractor.VectorSize);

            pipeline.ReduceFeaturesByThreshold(0.2f);

            Console.WriteLine(pipeline.FeatureExtractor.VectorSize);

            var fitnessFunction = MultilayerNetworkFitnessFunctions.ClassificationAccuracyFunction(testSet);

            var classifier = pipeline.ToMultilayerNetworkClassifier(c => c.Character, 0.45f).Execute();

            var errors = new List<List<char>>();

            foreach(var testChar in testSet)
            {
                var results = classifier.Classify(testChar.ObjectInstance).ToList();

                if (results.Count > 0)
                {
                    var first = results.First();

                    if (!first.ClassType.Equals(testChar.ObjectInstance))
                    {
                        var cluster = errors.FirstOrDefault(e => e.Contains(testChar.Classification) || e.Contains(first.ClassType));

                        if (cluster == null)
                        {
                            cluster = new List<char>();
                            errors.Add(cluster);
                        }

                        cluster.Add(testChar.Classification);
                        cluster.Add(first.ClassType);
                    }
                }
            }

            foreach(var err in errors)
            {
                Console.WriteLine(err);
            }
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
