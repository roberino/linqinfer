using LinqInfer.Data;
using LinqInfer.Maths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LinqInfer.Learning.Features
{
    internal class MultiStrategyFeatureExtractor<T> : 
        IFloatingPointFeatureExtractor<T>,
        IExportableAsVectorDocument,
        IImportableAsVectorDocument
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

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            throw new System.NotImplementedException();
        }

        public void Load(Stream input)
        {
            var xml = XDocument.Load(input);
            var doc = new BinaryVectorDocument(xml);

            foreach (var item in doc
                .Children
                .Zip(
                    _featureExtractionStrategies,
                    (c, s) => new { strat = s, child = c }))
            {
                item.child.ReadChildObject(item.strat);
            }
        }

        public void Save(Stream output)
        {
            var doc = new BinaryVectorDocument();

            doc.SetType(this);

            foreach (var fe in _featureExtractionStrategies)
            {
                doc.WriteChildObject(fe);
            }

            doc.ExportAsXml().Save(output);
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            throw new System.NotImplementedException();
        }
    }
}