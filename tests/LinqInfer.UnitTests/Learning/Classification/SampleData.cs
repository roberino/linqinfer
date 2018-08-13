using System.Collections.Generic;
using System.Linq;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;

namespace LinqInfer.UnitTests.Learning.Classification
{
    static class SampleData
    {
        public static IEnumerable<TrainingPair<IVector, IVector>> SetupData()
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
