using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;

namespace LinqInfer.Text.Analysis
{
    public sealed class VectorExtractionResult<T>
    {
        public VectorExtractionResult(INetworkClassifier<string, T> model, LabelledMatrix<string> vectors)
        {
            Model = model;
            Vectors = vectors;
        }

        public INetworkClassifier<string, T> Model { get; }

        public LabelledMatrix<string> Vectors { get; }
    }
}