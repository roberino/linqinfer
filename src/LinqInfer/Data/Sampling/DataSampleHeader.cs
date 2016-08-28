using System;

namespace LinqInfer.Data.Sampling
{
    public class DataSampleHeader : IStorageObject
    {
        private IUriProvider _uriProvider;

        public DataSampleHeader()
        {
            Key = Guid.NewGuid().ToString();
            Created = DateTime.UtcNow;
            Modified = DateTime.UtcNow;
            Summary = new SampleSummary();
            Metadata = new DataSampleMetadata();
            _uriProvider = new UriProvider();
        }

        public long Id { get; set; }

        public virtual Uri Uri
        {
            get
            {
                if (_uriProvider == null) _uriProvider = new UriProvider();
                return _uriProvider.Create("samples", Key);
            }
        }

        public string Key { get; set; }

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
