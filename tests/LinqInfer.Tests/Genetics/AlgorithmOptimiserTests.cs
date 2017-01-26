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
        public void Optimise_InverseRelationship_OptimisedAsExpected()
        {
            var ao = new AlgorithmOptimiser();

            var x = ao.Parameters.DefineDouble("x", 0, 100, 50);
            var y = ao.Parameters.DefineDouble("y", 0, 100, 50);

            var bestParams = ao.Optimise(p =>
            {
                var r = x / y;

                // Console.WriteLine("{0}/{1}={2}", x.CurrentValue, y.CurrentValue, r);

                return r;
            }, 50);

            Console.WriteLine("x={0},y={1}", x.OptimalValue, y.OptimalValue);

            Assert.That(x.OptimalValue, Is.GreaterThan(90));
            Assert.That(y.OptimalValue, Is.LessThan(10));
        }

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
        [Category("Build-Omit")]
        public void Optimise_CategoricalParameter_FindsBestValueOverNIterations()
        {
            var ao = new AlgorithmOptimiser();

            var x = ao.Parameters.DefineCategoricalVariable("x", 'a', 'a', 'b', 'c');
            var y = ao.Parameters.DefineDouble("y", 1, 2, 1.5);

            var bestParams = ao.Optimise(p =>
            {
                switch (x)
                {
                    case 'a':
                        return 0.5;
                    case 'b':
                        return 1;
                    default:
                        return 0;
                }
            }, 150);

            Assert.That(x.OptimalValue, Is.EqualTo('b'));
        }

        [Test]
        public void Optimise_CategoricalParameterAndDouble_FindsBestValueOverNIterations()
        {
            var ao = new AlgorithmOptimiser();

            var x = ao.Parameters.DefineCategoricalVariable("x", 'a', 'a', 'b', 'c');
            var y = ao.Parameters.DefineDouble("y", 0, 5, 0.5);

            var bestParams = ao.Optimise(p =>
            {
                // Console.WriteLine("{0}/{1}", x, y);

                switch (x)
                {
                    case 'a':
                        return 0.5;
                    case 'b':
                        return 1;
                    default:
                        return y;
                }
            }, 150);

            Assert.That(x.OptimalValue, Is.EqualTo('c'));
            Assert.That(y.OptimalValue, Is.GreaterThan(4d));
        }

        [Test]
        public void Optimise_CategoricalParameterAndDouble2_FindsBestValueOverNIterations()
        {
            var ao = new AlgorithmOptimiser();

            var x = ao.Parameters.DefineCategoricalVariable("x", 'a', 'a', 'b', 'c');
            var y = ao.Parameters.DefineDouble("y", 0, 5, 0.5);

            var bestParams = ao.Optimise(p =>
            {
                // Console.WriteLine("{0}/{1}", x, y);

                switch (x)
                {
                    case 'a':
                        return 0.5;
                    case 'b':
                        return 6;
                    default:
                        return y;
                }
            }, 150);

            Assert.That(x.OptimalValue, Is.EqualTo('b'));
        }

        [Test]
        public void Optimise_MoreComplexExpRelationship_MutatesBasedOnHighestVariance()
        {
            var ao = new AlgorithmOptimiser();

            var x = ao.Parameters.DefineDouble("x", 4, 102, 55);
            var y = ao.Parameters.DefineDouble("y", 43, 300, 120);

            int xm = 0; int ym = 0;

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

                xm += x.MutationCounter;
                ym += y.MutationCounter;

                ao.Reset();
            }

            Assert.That(xm > 100);
            Assert.That(ym > 100);
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