using LinqInfer.Data;
using LinqInfer.Maths;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    public sealed class WordVector : IExportableAsVectorDocument
    {
        public WordVector(string word, long frequency = 1, IVector vector = null)
        {
            Word = word;
            Frequency = frequency;
            Vector = vector ?? new ZeroVector(1);
        }

        public string Word { get; }
        public IVector Vector { get; }
        public long Frequency { get; }
        public long NumberOfConnections => Vector.ToColumnVector().Where(v => v > 0).Count();

        public static WordVector FromVectorDocument(BinaryVectorDocument doc)
        {
            return new WordVector(
                doc.PropertyOrDefault(nameof(Word), string.Empty),
                doc.PropertyOrDefault(nameof(Frequency), default(long)),
                doc.Vectors.Single());
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            doc.SetPropertyFromExpression(() => Word);
            doc.SetPropertyFromExpression(() => Frequency);
            doc.Vectors.Add(Vector);

            return doc;
        }
    }
}