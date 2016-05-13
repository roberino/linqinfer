using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Data.Sampling
{
    [Serializable]
    public class DataSampleHeader : IStorageObject
    {
        [NonSerialized]
        private IUriProvider _uriProvider;

        public DataSampleHeader()
        {
            Id = Guid.NewGuid().ToString();
            Created = DateTime.UtcNow;
            Modified = DateTime.UtcNow;
            Summary = new SampleSummary();
            Metadata = new DataSampleMetadata();
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

        public string Label { get; set; }

        public string SourceDataUrl { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public virtual SampleSummary Summary { get; set; }

        public virtual DataSampleMetadata Metadata { get; set; }
    }
}
