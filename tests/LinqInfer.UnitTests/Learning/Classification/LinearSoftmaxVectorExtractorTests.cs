//using System.Linq;
//using LinqInfer.Learning.Classification;
//using LinqInfer.Maths;
//using NUnit.Framework;

//namespace LinqInfer.UnitTests.Learning.Classification
//{
//    [TestFixture]
//    public class LinearSoftmaxVectorExtractorTests
//    {
//        [Test]
//        public void Evaluate_WhenGivenSample_ThenVectorReturned()
//        {
//            var sample1 = new ColumnVector1D(0.2, 0.3, 0.5, -0.2, -0.2);

//            var lsve = new LinearSoftmaxVectorExtractor(sample1.Size, 3, 2);

//            var result = lsve.Evaluate(sample1);

//            Assert.That(result, Is.AssignableTo<IVector>());
//        }

//        [Test]
//        public void Train_WhenGivenSample_ThenErrorGtZero()
//        {
//            var sample1 = new OneOfNVector(5, 3);
//            var targetOutput = new OneOfNVector(3, 1);

//            var lsve = new LinearSoftmaxVectorExtractor(sample1.Size, targetOutput.Size, 2);

//            var error = lsve.Train(sample1, targetOutput);

//            Assert.That(error, Is.GreaterThan(0));
//        }

//        [Test]
//        public void Train_WhenTrainedAndSampleEvaluatedAgain_ThenErrorIsReduced()
//        {
//            var sample1 = new OneOfNVector(5, 3);
//            var targetOutput = new OneOfNVector(3, 1);

//            var lsve = new LinearSoftmaxVectorExtractor(sample1.Size, targetOutput.Size, 2);

//            var error0 = lsve.CalculateError(sample1, targetOutput);

//            foreach (var x in Enumerable.Range(0, 100))
//            {
//                lsve.Train(sample1, targetOutput);
//            }

//            var error1 = lsve.CalculateError(sample1, targetOutput);

//            Assert.That(error0, Is.GreaterThan(error1));
//        }
//    }
//}