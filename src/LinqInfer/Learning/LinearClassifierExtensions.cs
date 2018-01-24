using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Learning
{
    public static class LinearClassifierExtensions
    {
        public static async Task<LinearSoftmaxClassifier> CreateLinearClassifier<TInput, TClass>
            (this IAsyncTrainingSet<TInput, TClass> trainingSet, Action<LearningParameters> config = null)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            var parameters = new LearningParameters();

            config?.Invoke(parameters);

            var classifier = new LinearSoftmaxClassifier(trainingSet.FeaturePipeline.FeatureExtractor.VectorSize, trainingSet.OutputMapper.VectorSize, parameters.LearningRate);

            await trainingSet
                .ExtractInputOutputIVectorBatches()
                .ProcessUsing(b =>
                {
                    var err = classifier.Train(b.Items, parameters.MinimumError, 1);

                    return err > parameters.MinimumError;
                });

            return classifier;
        }

        public static IObjectClassifier<TClass, TInput> AttachLinearSoftmaxClassifier<TInput, TClass>(
            this IAsyncTrainingSet<TInput, TClass> trainingSet, float learningRate = 0.1f, float minError = 0.001f)
             where TInput : class
            where TClass : IEquatable<TClass>
        {
            var classifier = new LinearSoftmaxClassifier(trainingSet.FeaturePipeline.FeatureExtractor.VectorSize, trainingSet.OutputMapper.VectorSize, learningRate);

            var adapter = new AsyncTrainingAdapter<TInput, TClass, LinearSoftmaxClassifier>(classifier, (n, e) => true);

            trainingSet.RegisterSinks(adapter);

            return new GenericClassifier<TInput, TClass>(trainingSet.FeaturePipeline.FeatureExtractor, trainingSet.OutputMapper, classifier.Evaluate);
        }
    }
}