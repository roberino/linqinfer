using LinqInfer.Math;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace LinqInfer.Tests
{
    public class TestFixtureBase : AssertionHelper
    {
        public static void AssertEquiv(Fraction x, double y, int precision = 6)
        {
            Console.WriteLine("{0}={1}~={2}", x, x.Value, y);
            Assert.That(System.Math.Round(x.Value, precision), Is.EqualTo(System.Math.Round(y, precision)));
        }

        public static Stream GetResource(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            var rname = asm.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(name));

            return asm.GetManifestResourceStream(rname);
        }
    }
}
