using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace LinqInfer.IntegrationTests.Maths
{
    [TestFixture]
    public class MandelbrotSet : TestFixtureBase
    {
        XDocument GetTemplate()
        {
            return GetResourceAsXml("CanvasTemplate.html");
        }

        [Test]
        public void GenerateMandelbrotSet()
        {
            const int dim = 100;
            var threshold = 1.634d;
            double scale = threshold / dim;
            
            var htmlString = new StringBuilder();

            var htmlFormatter = new Action<double, double, double, double>((x, y, v, c) =>
            {
                var b = (byte)Math.Min((c * 200d), 200);
                var r = (byte)Math.Min((v * 200d), 200);
                //var hex = "#0000" + b.ToString("X2");
                //htmlString.AppendLine($"ctx.fillStyle = '{hex}';");
                //htmlString.AppendLine($"ctx.fillRect({x}, {y}, 1, 1);");
                htmlString.AppendLine($"drawPixel({x}, {y}, {r}, 0, {b}, 255);");
            });

            var generator = new Action<int, int, double>((s, f, p) =>
            {
                foreach (var m in Enumerable.Range(s, f))
                {
                    double c0 = (dim / 2d - m) * scale;

                    foreach (var n in Enumerable.Range(s, f))
                    {
                        var x0 = Complex.Zero;
                        double c1 = (n - dim / 2d) * scale;

                        var c = new Complex(c1, c0);
                        var fi = new FunctionIterator<Complex>(x => Complex.Pow(x, p) + c);

                        var results = fi.IterateFunction(x0, 10, 5, (i, x) => Print("   {0},{1}", i, x.Magnitude), x => x.Magnitude > threshold);

                        if (results.WasHalted)
                        {
                            htmlFormatter(n, m, Math.Exp(-Math.Abs(results.Outputs.First().Magnitude)), results.ActualIterations / (double)results.MaxIterations);
                        }
                    }
                }
            });

            generator(1, dim, 0.3);
            generator(1, dim, 0.8);
            generator(1, dim, 1.2);
            generator(1, dim, 1.9);
            generator(1, dim, 2.5);
            generator(1, dim, 3.1);

            var doc = GetTemplate();

            var script = doc.Root.Element("head").Element("script");
            var data = script.Element("vectorData");

            data.ReplaceWith(new XText(htmlString.ToString()));

            SaveArtifact("mandelbrot.html", doc.Save);
        }

        void Print(string msg, params object[] args)
        {

        }
    }
}