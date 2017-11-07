using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class LinearSeparabilityExample
    {
        [Test]
        public void Normal_LinearSeparableTwoClass_ExampleDataSet_ToMulti()
        {
            var dataX0 = Functions.NormalRandomDataset(3, 10);
            var dataX1 = Functions.NormalRandomDataset(0.6, 78);
            var dataY0 = Functions.NormalRandomDataset(2, 98);
            var dataY1 = Functions.NormalRandomDataset(7, 12);
            var testX0 = Functions.NormalRandomDataset(3, 10);
            var testY0 = Functions.NormalRandomDataset(2, 98);

            var c0 = dataX0.Zip(dataY0, (x, y) => new
            {
                x = x,
                y = y,
                cls = "C0"
            });

            var ctest0 = testX0.Zip(testY0, (x, y) => new
            {
                x = x,
                y = y,
                cls = "C0"
            }).AsQueryable().CreatePipeline().AsTrainingSet(x => x.cls);

            var c1 = dataX1.Zip(dataY1, (x, y) => new
            {
                x = x,
                y = y,
                cls = "C1"
            });

            var pipeline = c0.Concat(c1).AsQueryable().CreatePipeline(v => new[] { v.x, v.y }, 2);

            // pipeline.ToCsv(Console.Out, x => x.cls).Execute();

            var classifier = pipeline.ToMultilayerNetworkClassifier(c => c.cls).Execute();

            Console.Write("Classifier created");
           
            var score = classifier.ClassificationAccuracyPercentage(ctest0);

            Console.WriteLine(score);

            Assert.That(score, Is.GreaterThan(0));
        }
    }
}
