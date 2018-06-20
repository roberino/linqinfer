using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LinqInfer.Maths;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace LinqInfer.UnitTests
{
    public class TestFixtureBase
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

        public static Assembly GetAssembly()
        {
#if NET_STD
            return typeof(TestFixtureBase).GetTypeInfo().Assembly;
#else
            return Assembly.GetExecutingAssembly();
#endif
        }

        public static Stream GetResource(string name)
        {
            var asm = GetAssembly();
            var names = asm.GetManifestResourceNames();
            var rname = names.FirstOrDefault(r => r.EndsWith(name));

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

        public static void SaveArtifact(string fileName, Action<Stream> writer)
        {
            const string artifactPath = "../../../../../artifacts";
            using (var fs = new FileStream(Path.Combine(artifactPath, fileName), FileMode.Create))
            {
                writer(fs);
            }
        }

        protected string RemoveWhitespace(object value, string replaceWith = "_")
        {
            if (value == null) return null;

            return Regex.Replace(value.ToString(), @"\s+", m => "_", RegexOptions.Multiline);
        }

        protected void LogVerbose(string format, params object[] args)
        {
#if DEBUG
            Console.WriteLine(format, args);
#endif
        }
    }
}
