using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class RandomTests
    {
        [TestCase(1000)]
        public void NextDouble(int iterations)
        {
            var rnd = new LinqInfer.Maths.Random();
            double total = 0;
            double first = 0;

            foreach(var n in Enumerable.Range(1, iterations))
            {
                var x = rnd.NextDouble();

                if (total == 0) first = x;

                 total += x;

                // Console.WriteLine(x);
            }

            var ave = total / iterations;

            Console.WriteLine("Ave:{0}", ave);

            Assert.That(ave, Is.AtLeast(0.4d));
            Assert.That(ave, Is.AtMost(0.6d));
            Assert.That(ave, Is.Not.EqualTo(first));
        }

        [TestCase(1000)]
        public void NextInt(int iterations)
        {
            var rnd = new LinqInfer.Maths.Random();
            int total = 0;

            foreach (var n in Enumerable.Range(1, iterations))
            {
                var x = rnd.Next(100);
                total += x;

                //Console.WriteLine(x);
            }

            var ave = total / (double)iterations;

            Console.WriteLine("Ave:{0}", ave);

            Assert.That(ave, Is.AtLeast(36d));
            Assert.That(ave, Is.AtMost(64d));
        }

        [Test]
        public void ByteTest()
        {
            Write("Max int", int.MaxValue, BitConverter.GetBytes(int.MaxValue));
            Write("Max long", long.MaxValue, BitConverter.GetBytes(long.MaxValue));
            Write("Zero int", 0, BitConverter.GetBytes(0));
            Write("Min int", int.MinValue, BitConverter.GetBytes(int.MinValue));
            Write("Max double", double.MaxValue, BitConverter.GetBytes(double.MaxValue));
            Write("Zero double", 0d, BitConverter.GetBytes(0d));
            Write("1 double", 1d, BitConverter.GetBytes(1d));

            var b = BitConverter.GetBytes(int.MaxValue);
            b[sizeof(int) - 1] = (byte)(b[sizeof(int) - 1] - 1);

            Write("Max int - 1", int.MaxValue - 1, b);
        }

        private void Write(string label, object val, byte[] data)
        {
            Console.Write(label);
            Console.Write(" ");
            Console.Write(val);
            Console.Write(": ");
            foreach(var bit in data)
            {
                Console.Write(bit + ",");
            }
            Console.Write(" ({0} bytes)", data.Length);
            Console.WriteLine();
        }
    }
}
