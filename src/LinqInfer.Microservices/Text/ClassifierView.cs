using LinqInfer.Learning.Classification;
using LinqInfer.Microservices.Resources;
using LinqInfer.Text;

namespace LinqInfer.Microservices.Text
{
    public class ClassifierView : ResourceHeader
    {
        private readonly IDynamicClassifier<string, TokenisedTextDocument> _classifier;

        public ClassifierView(IDynamicClassifier<string, TokenisedTextDocument> classifier)
        {
            _classifier = classifier;
        }

        public ClassifierStats Statistics
        {
            get
            {
                return _classifier.Statistics;
            }
        }
    }
}