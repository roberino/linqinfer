using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Learning
{
    public static class LinearClassifierExtensions
    {
        public static async Task<LinearClassifier> CreateLinearClassifier<TInput, TClass>
            (this IAsyncTrainingSet<TInput, TClass> trainingSet, float learningRate = 0.1f, float minError = 0.001f)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            var classifier = new LinearClassifier(trainingSet.FeaturePipeline.FeatureExtractor.VectorSize, trainingSet.OutputMapper.VectorSize, learningRate);

            await trainingSet
                .ExtractInputOutputIVectorBatches()
                .ProcessUsing(b =>
                {
                    var err = classifier.Train(b.Items, minError, 1);

                    return err > minError;
                });

            return classifier;
        }
    }
}