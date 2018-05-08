using LinqInfer.Data;
using LinqInfer.Maths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Features
{
    internal class MultiStrategyFeatureExtractor<T> : 
        IFloatingPointFeatureExtractor<T>,
        IExportableAsDataDocument,
        IImportableFromDataDocument
    {
        private readonly IFloatingPointFeatureExtractor<T>[] _featureExtractionStrategies;

        public MultiStrategyFeatureExtractor(IFloatingPointFeatureExtractor<T>[] featureExtractionStrategies)
        {
            _featureExtractionStrategies = featureExtractionStrategies;
        }

        public int VectorSize => _featureExtractionStrategies.Sum(s => s.VectorSize);

        public IEnumerable<IFeature> FeatureMetadata => _featureExtractionStrategies.SelectMany(s => s.FeatureMetadata);

        public IVector ExtractIVector(T obj)
        {
            var vects = _featureExtractionStrategies.Select(f => f.ExtractIVector(obj));

            return new MultiVector(vects.ToArray());
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public void ImportData(PortableDataDocument doc)
        {
            for (var i = 0; i < _featureExtractionStrategies.Length; i++)
            {
                doc.ReadChildObject(_featureExtractionStrategies[i], i);
            }
        }

        public void Load(Stream input)
        {
            var xml = XDocument.Load(input);
            var doc = new PortableDataDocument(xml);

            ImportData(doc);
        }

        public void Save(Stream output)
        {
            ExportData().ExportAsXml().Save(output);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetType(this);

            foreach (var fe in _featureExtractionStrategies)
            {
                doc.WriteChildObject(fe);
            }

            return doc;
        }
    }
}