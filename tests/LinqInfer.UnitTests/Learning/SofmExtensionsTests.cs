using LinqInfer.Learning;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Learning
{
    [TestFixture]
    public class SofmExtensionsTests
    {
        [Test]
        public void ToSofm_WithoutSupplingRadius()
        {
            var sofm = Enumerable.Range(1, 10)
                .Select(n => Functions.RandomVector(2))
                .AsQueryable()
                .CreatePipeline()
                .ToSofm(3)
                .Execute();

            Assert.That(sofm.Count(), Is.GreaterThan(0));
            Assert.That(sofm.Count(), Is.LessThanOrEqualTo(3));
        }

        [Test]
        public async Task ToSofm_WithAnInitialRadius()
        {
            var sofm = Enumerable.Range(1, 10)
                .Select(n => Functions.RandomVector(2))
                .AsQueryable()
                .CreatePipeline()
                .ToSofm(3, 0.2f, 0.1f, 100)
                .Execute();

            Assert.That(sofm.Count(), Is.GreaterThan(0));
            Assert.That(sofm.Count(), Is.LessThanOrEqualTo(3));

            var xml = await (await sofm.ExportNetworkTopologyAsync()).ExportAsGexfAsync();

            Console.Write(xml);
        }
    }
}