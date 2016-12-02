using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using LinqInfer.Maths;
using NUnit.Framework.Constraints;

namespace LinqInfer.Tests
{
    public class TestFixtureBase : AssertionHelper
    {
        public static void AssertEquiv(Fraction x, double y, int precision = 6)
        {
            Console.WriteLine("{0}={1}~={2}", x, x.Value, y);
            Assert.That(Math.Round(x.Value, precision), Is.EqualTo(Math.Round(y, precision)));
        }

        public static RangeConstraint IsAround(double expected)
        {
            return Is.InRange(expected - 0.00001d, expected + 0.00001d);
        }

        public static Stream GetResource(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            var rname = asm.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(name));

            return asm.GetManifestResourceStream(rname);
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
