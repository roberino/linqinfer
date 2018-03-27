using LinqInfer.Maths;

namespace LinqInfer.Text.Analysis
{
    public sealed class WordVector
    {
        public WordVector(string word, IVector vector)
        {
            Word = word;
            Vector = vector;
        }

        public string Word { get; }
        public IVector Vector { get; }
    }
}