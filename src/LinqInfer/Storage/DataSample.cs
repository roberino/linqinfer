using LinqInfer.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Storage
{
    [Serializable]
    public class DataSample : IStorageObject
    {
        [NonSerialized]
        private IUriProvider _uriProvider;

        public DataSample()
        {
            Id = Guid.NewGuid().ToString();
            Created = DateTime.UtcNow;
            Modified = DateTime.UtcNow;
            Summary = new SampleSummary();
            Indexes = new List<FieldIndex>();
            _uriProvider = new UriProvider();
        }

        public virtual Uri Uri
        {
            get
            {
                if (_uriProvider == null) _uriProvider = new UriProvider();
                return _uriProvider.Create("samples", Id);
            }
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string SourceDataUrl { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public virtual SampleSummary Summary { get; set; }

        public virtual SampleSummary Recalculate()
        {
            if (SampleData != null)
            {
                if (Summary == null) Summary = new SampleSummary();

                var data = SampleData.Select(d => d.AsColumnVector()).ToList();

                Summary.Min = data.Select(x => x.EuclideanLength).Min();
                Summary.Max = data.Select(x => x.EuclideanLength).Max();

                var muStdDev = Functions.MeanStdDev(data.Select(x => x.EuclideanLength));

                Summary.Mean = muStdDev.Item1;
                Summary.StdDev = muStdDev.Item2;
                Summary.Count = SampleData.Count;

                if (SampleData.Count > 0)
                {
                    var fv = SampleData.First().FeatureVector;
                    Summary.Size = fv == null ? 0 : fv.Length;
                }
            }

            return Summary;
        }

        public virtual ICollection<FieldIndex> Indexes { get; set; }

        public virtual ICollection<DataItem> SampleData { get; set; }
    }
}
