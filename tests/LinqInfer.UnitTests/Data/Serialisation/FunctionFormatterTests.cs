using LinqInfer.Data.Serialisation;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.UnitTests.Data.Serialisation
{
    [TestFixture]
    public class FunctionFormatterTests
    {
        [Test]
        public void Format_WhenGivenFunction_ThenReturnsFunctionNameAndParams()
        {
            var formatter = new FunctionFormatter();

            var str = formatter.Format(new Func1(7, 3), f => new object[] { f.X, f.Y });

            Assert.That(str, Is.EqualTo("Func1(7,3)"));
        }

        [Test]
        public void Create_WhenGivenTypeAndFormattedFunction_ThenConstructorInvoked()
        {
            var formatter = new FunctionFormatter();

            var input = "Func1(7,3)";

            var instance = formatter.Create<Func1>(input);

            Assert.That(instance.X, Is.EqualTo(7));
            Assert.That(instance.Y, Is.EqualTo(3));
        }

        [Test]
        public void Create_WhenGivenStaticMethod_ThenCanBeCreated()
        {
            var formatter = new FunctionFormatter();

            var input = "CreateNew(5,3)";

            var instance = formatter.Create<Func1>(input);

            Assert.That(instance.X, Is.EqualTo(5));
            Assert.That(instance.Y, Is.EqualTo(3));
        }

        [Test]
        public void Bind_WhenGivenInstance_ThenFunctionInvoked()
        {
            var formatter = new FunctionFormatter();

            var input = "GetValues(5)";
            var instance = new Func1(8, 12);

            var output = formatter.BindToInstance<Func1, double[]>(input, instance);

            var expectedOutput = instance.GetValues(5);

            Assert.AreEqual(output.Length, expectedOutput.Length);

            foreach (var item in output.Zip(expectedOutput, (o, e) => new { o, e }))
            {
                Assert.AreEqual(item.o, item.e);
            }
        }

        public class Func1
        {
            public Func1(double x)
            {
                X = x;
                Y = 12;
            }

            public Func1(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X { get; }

            public double Y { get; }

            public double Execute() => X * Y;

            public static Func1 CreateNew(double x, double y)
            {
                return new Func1(x, y);
            }

            public double[] GetValues(int a)
            {
                return new[] { X * a, Y };
            }
        }
    }
}
