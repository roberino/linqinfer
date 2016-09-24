using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class MonteCarloSimulationTests : TestFixtureBase
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Simulate_PI(bool parallel)
        {
            var mcs = new MonteCarloSimulation(v =>
            {
                var x = v[0];
                var y = v[1];

                return (x * x) + (y * y) <= 1;

            }, 2, e => e * 4);

            mcs.ParallelProcess = parallel;

            var estimate = mcs.Simulate(100000);

            Console.WriteLine(mcs.ToString());

            Assert.That(mcs.CurrentEstimate.Value, Is.EqualTo(estimate));
            Assert.That(mcs.BestEstimate.Value, Is.EqualTo(estimate));
            Assert.That(mcs.Elapsed.TotalMilliseconds, Is.GreaterThan(1));

            Assert.That(estimate.Value > 2.5 && estimate.Value < 4.5);
        }
    }
}