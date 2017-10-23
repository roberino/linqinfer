using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqInfer.Tests.Learning.Classification
{
    [TestFixture]
    public class LinearClassifierTests
    {
        [Test]
        public void Train_RandomNormalDataset_ClassifiesAsExpected()
        {
            var classifier = new LinearClassifier(2, 2);
            var trainingData = SetupData();

            var lastErr = 0d;
            var nt = 0;

            classifier.Train(trainingData, (n, e) =>
            {
                lastErr = e;
                nt = n;
                return n > 100 || e > lastErr;
            });

            double totalDiff = 0;
            int i = 0;

            foreach(var test in SetupData())
            {
                var output = classifier.Evaluate(test.Input);
                var diff = test.TargetOutput.ToColumnVector().CosineDistance(output);

                Console.Write($"diff: {diff} exp: {test.TargetOutput} act: {output}");

                totalDiff += diff;
                i++;
            }

            var meanDiff = totalDiff / i;

            Assert.That(meanDiff, Is.LessThan(0.39));
        }

        private IEnumerable<TrainingPair<IVector, IVector>> SetupData()
        {
            var dataX0 = Functions.NormalRandomDataset(0.02, 0.1);
            var dataY0 = Functions.NormalRandomDataset(0.01, 0.98);

            var dataX1 = Functions.NormalRandomDataset(0.05, 0.78);
            var dataY1 = Functions.NormalRandomDataset(0.015, 0.12);

            var sample0 = dataX0.Zip(dataY0, (x, y) => new ColumnVector1D(new[] { x, y }));
            var output0 = new OneOfNVector(2, 0);

            var sample1 = dataX1.Zip(dataY1, (x, y) => new ColumnVector1D(new[] { x, y }));
            var output1 = new OneOfNVector(2, 1);

            var samplesOf0 = sample0.Select(s => new TrainingPair<IVector, IVector>(s, output0));
            var samplesOf1 = sample1.Select(s => new TrainingPair<IVector, IVector>(s, output1));

            return samplesOf0.Concat(samplesOf1).RandomOrder();
        }
    }
}
