using LinqInfer.Genetics;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Genetics
{
    [TestFixture]
    public class AlgorithmOptimiserTests
    {
        [Test]
        public void Optimise_SimpleLogarithmicRelationship_MutatesBasedOnHighestVariance()
        {
            var ao = new AlgorithmOptimiser();

            var x = ao.Parameters.DefineDouble("x", 4, 102, 65);
            var y = ao.Parameters.DefineDouble("y", 43, 77, 59.2);

            var bestParams = ao.Optimise(p =>
            {
                var xv = p.GetValue<double>("x");

                return Math.Log(xv);
            }, 50);

            Assert.That(y.MutationCounter < x.MutationCounter);
        }

        [Test]
        public void Optimise_MoreComplexExpRelationship_MutatesBasedOnHighestVariance()
        {
            var ao = new AlgorithmOptimiser();

            var x = ao.Parameters.DefineDouble("x", 4, 102, 55);
            var y = ao.Parameters.DefineDouble("y", 43, 300, 120);

            int xm = 0;

            foreach (var n in Enumerable.Range(0, 20))
            {
                ao.Optimise(p =>
                {
                    var xv = p.GetValue<double>("x");
                    var yv = p.GetValue<double>("y");

                    return Math.Pow(xv, 3) - Math.Pow(yv, 2);
                }, 50);

                Console.WriteLine("Mutation Counter = x{0} y{1}", x.MutationCounter, y.MutationCounter);
                Console.WriteLine("Optimal Values = x{0} y{1}", x.OptimalValue, y.OptimalValue);

                if (x.MutationCounter > y.MutationCounter) xm++;

                ao.Reset();
            }

            Assert.That(xm > 5);
        }

        [Test]
        public void MutatableDoubleParameter_InitialisesAndBehavesAsExpected()
        {
            var md = new MutatableDoubleParameter(100, 5, 200);

            Assert.That(md.CurrentValue, Is.EqualTo(100d));
            Assert.That(md.MutationCounter, Is.EqualTo(0));
            Assert.That(md.MaxValue, Is.EqualTo(200));
            Assert.That(md.MinValue, Is.EqualTo(5));
            Assert.That(md.Type, Is.EqualTo(TypeCode.Double));

            md.Score(5);
            md.Mutate();

            Assert.That(md.WasMutated);
            Assert.That(md.MutationCounter, Is.EqualTo(1));

            foreach(var n in Enumerable.Range(1, 10))
            {
                var v = (double)md.CurrentValue;
                md.Score(v * 2);
                md.Mutate();
            }

            Assert.That(md.CurrentValue, Is.GreaterThan(150));

            md.Reset();

            Assert.That(md.MutationCounter, Is.EqualTo(0));
            Assert.That(md.CurrentValue, Is.EqualTo(100));
        }
    }
}