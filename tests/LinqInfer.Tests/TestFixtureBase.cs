using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using LinqInfer.Maths;
using NUnit.Framework.Constraints;
using System.Diagnostics;
using System.Xml.Linq;

namespace LinqInfer.Tests
{
    public class TestFixtureBase : AssertionHelper
    {
        public static TimeSpan TimeTest(Action test, string name = null)
        {
            var sw = new Stopwatch();

            sw.Start();

            test();

            sw.Stop();

            Console.WriteLine("Invoke {0} took {1}", name ?? test.Method.Name, sw.Elapsed);

            return sw.Elapsed;
        }

        public static void AssertEquiv(Fraction x, double y, int precision = 6)
        {
            Console.WriteLine("{0}={1}~={2}", x, x.Value, y);
            Assert.That(Math.Round(x.Value, precision), Is.EqualTo(Math.Round(y, precision)));
        }

        public static RangeConstraint IsAround(double expected, double maxErr = 0.00001d)
        {
            return Is.InRange(expected - maxErr, expected + maxErr);
        }

        public static Stream GetResource(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            var rname = asm.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(name));

            return asm.GetManifestResourceStream(rname);
        }

        public static XDocument GetResourceAsXml(string name)
        {
            using (var stream = GetResource(name))
            {
                return XDocument.Load(stream);
            }
        }

        public static byte[] GetResourceAsBytes(string name)
        {
            using (var httpHeaderStream = GetResource(name))
            using (var ms = new MemoryStream())
            {
                httpHeaderStream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
