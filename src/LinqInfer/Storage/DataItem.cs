using LinqInfer.Learning.Features;
using LinqInfer.Probability;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace LinqInfer.Storage
{
    [Serializable]
    public class DataItem : IStorageObject
    {
        [NonSerialized]
        private IUriProvider _uriProvider;
        [NonSerialized]
        private ColumnVector1D _vector;
        [NonSerialized]
        private object _item;

        public DataItem(IUriProvider uriProvider = null)
        {
            _uriProvider = uriProvider ?? new UriProvider();
            Id = Guid.NewGuid().ToString();
            Item = new object();
        }

        public string Id { get; set; }

        public string Label { get; set; }

        public virtual object Item
        {
            get
            {
                return _item;
            }
            set
            {
                _item = value;
            }
        }

        public double[] FeatureVector { get; set; }

        public virtual void ExtractFeatures(IFloatingPointFeatureExtractor<object> featureExtractor)
        {
            if (Item != null)
            {
                FeatureVector = featureExtractor.ExtractVector(Item).Select(v => (double)v).ToArray();
            }
        }

        public ColumnVector1D AsColumnVector()
        {
            if (_vector == null && FeatureVector != null)
            {
                _vector = new ColumnVector1D(FeatureVector);
            }

            return _vector;
        }

        public virtual Uri Uri
        {
            get
            {
                if (_uriProvider == null) _uriProvider = new UriProvider();
                return _uriProvider.Create("item", Id);
            }
        }

        public override string ToString()
        {
            return Label == null ? Uri.ToString() : Label;
        }
    }
}
