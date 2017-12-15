using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Text.VectorExtraction;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    internal class WordVectorExtractor
    {
        public LabelledMatrix<string> Extract(ContinuousBagOfWords cbow, ISemanticSet widerVocabulary, int sampleSize = 10000)
        {
            var data = cbow.SelectMany(c =>
                    c.ContextualWords
                    .Select(w => new WordPair() { WordA = w.Text, WordB = c.TargetWord.Text })
                   )
                   .Take(sampleSize);

            var encoder = new OneHotTextEncoding<WordPair>(widerVocabulary, t => t.WordA);

            var pipeline = data.AsQueryable().CreatePipeline(encoder);

            var trainingSet = pipeline.AsTrainingSet(t => t.WordB);

            var classifier = new LinearSoftmaxClassifier(trainingSet.FeaturePipeline.VectorSize, trainingSet.OutputMapper.VectorSize);

            classifier.Train(trainingSet.ExtractTrainingVectorBatches().SelectMany(b => b), 0.0001f);

            return new LabelledMatrix<string>(classifier.Vectors, trainingSet.OutputMapper.FeatureMetadata.ToDictionary(m => m.Label, m => m.Index));
        }
    }
}