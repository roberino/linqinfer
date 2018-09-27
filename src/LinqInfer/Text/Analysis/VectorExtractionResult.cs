using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;

namespace LinqInfer.Text.Analysis
{
    public sealed class VectorExtractionResult : IExportableAsDataDocument
    {
        internal VectorExtractionResult(PortableDataDocument model, LabelledMatrix<string> vectors)
        {
            ModelData = model;
            Vectors = vectors;
        }

        public PortableDataDocument ModelData { get; }

        public LabelledMatrix<string> Vectors { get; }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.Children.Add(ModelData);
            doc.Children.Add(Vectors.ExportData());

            return doc;
        }

        public static VectorExtractionResult CreateFromData(PortableDataDocument data)
        {
            var matrix = new LabelledMatrix<string>(data.Children[1]);

            return new VectorExtractionResult(data.Children[0], matrix);
        }
    }
}