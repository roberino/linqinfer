using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Tests.Learning.Classification
{
    [TestFixture]
    public class LinearClassifierTests
    {
        [Test]
        public void Train_Simple2DExample()
        {
            var classifier = new LinearClassifier(2, 3);

            var trainingData1 = new TrainingPair<IVector, IVector>(ColumnVector1D.Create(0.9, 0.7), new OneOfNVector(3, 0));
            var trainingData2 = new TrainingPair<IVector, IVector>(ColumnVector1D.Create(0.01, 0.1), new OneOfNVector(3, 1));
            var trainingData3 = new TrainingPair<IVector, IVector>(ColumnVector1D.Create(-0.99, -0.6), new OneOfNVector(3, 2));

            classifier.Train(new[] { trainingData1, trainingData2, trainingData3 }, (n, e) =>
            {
                return n > 500;
            });

            var result = classifier.Evaluate(ColumnVector1D.Create(-0.8, -0.5));

            Assert.That(result[2], Is.GreaterThan(result[0]));
            Assert.That(result[2], Is.GreaterThan(result[1]));
        }

        [Test]
        public void Train_RandomNormalDataset_ClassifiesAsExpected()
        {
            var classifier = new LinearClassifier(2, 2);
            var trainingData = SetupData();

            var lastErr = new Queue<double>();
            var nt = 0;

            classifier.Train(trainingData, (n, e) =>
            {
                nt = n;

                Console.WriteLine(e);

                if (n > 20 && e > lastErr.Average()) return true;

                lastErr.Enqueue(e);

                if (lastErr.Count > 5) lastErr.Dequeue();

                return n > 300;
            });

            Console.WriteLine(nt);

            double totalDiff = 0;
            int i = 0;

            foreach (var test in SetupData())
            {
                var output = classifier.Evaluate(test.Input);
                var diff = test.TargetOutput.ToColumnVector().CosineDistance(output.ToColumnVector());

                Console.WriteLine($"diff: {diff} exp: {test.TargetOutput} act: {output}\n");
                
                totalDiff += diff;
                i++;
            }

            var meanDiff = totalDiff / i;

            Assert.That(meanDiff, Is.LessThan(0.39));
        }

        private IEnumerable<TrainingPair<IVector, IVector>> SetupData()
        {
            var dataX0 = Functions.NormalRandomDataset(0.02, 0.1);
            var dataY0 = Functions.NormalRandomDataset(0.01, -0.5);

            var dataX1 = Functions.NormalRandomDataset(0.05, 0.78);
            var dataY1 = Functions.NormalRandomDataset(0.015, 0.9);

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