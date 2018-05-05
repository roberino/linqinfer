using System;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class LinearClassifierTests
    {
        [Test]
        public void Train_Simple2DExample()
        {
            var classifier = new LinearSoftmaxClassifier(2, 3);

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
            var classifier = new LinearSoftmaxClassifier(2, 2);
            var trainingData = SampleData.SetupData();

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

            foreach (var test in SampleData.SetupData())
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
    }
}