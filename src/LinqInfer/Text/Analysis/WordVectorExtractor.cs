using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Text.VectorExtraction;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    internal class WordVectorExtractor
    {
        public LabelledMatrix<string> Extract(ContinuousBagOfWords cbow, ISemanticSet widerVocabulary, int sampleSize = 10000)
        {
            var data = cbow.GetNGrams().SelectMany(c =>
                    c.ContextualWords
                    .Select(w => new BiGram() { Input = w.Text, Output = c.TargetWord.Text })
                   )
                   .Take(sampleSize);

            var encoder = new OneHotTextEncoding<BiGram>(widerVocabulary, t => t.Input);

            var pipeline = data.AsQueryable().CreatePipeline(encoder);

            var trainingSet = pipeline.AsTrainingSet(t => t.Output);

            var classifier = new LinearSoftmaxClassifier(trainingSet.FeaturePipeline.VectorSize, trainingSet.OutputMapper.VectorSize);

            classifier.Train(trainingSet.ExtractTrainingVectorBatches().SelectMany(b => b), 0.0001f);

            return new LabelledMatrix<string>(classifier.Vectors, trainingSet.OutputMapper.FeatureMetadata.ToDictionary(m => m.Label, m => m.Index));
        }
    }
}