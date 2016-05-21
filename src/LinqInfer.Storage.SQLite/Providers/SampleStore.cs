using LinqInfer.Data.Sampling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Storage.SQLite.Providers
{
    public class SampleStore : StoreProvider, ISampleStore
    {
        public SampleStore(string dataDir) : base(dataDir) { }

        public override Task Setup()
        {
            return base.Setup();
        }

        public Task<DataSample> DeleteSample(Uri sampleUri)
        {
            throw new NotImplementedException();
        }

        public IQueryable<DataSampleHeader> ListSamples()
        {
            throw new NotImplementedException();
        }

        public Task<DataItem> RetrieveItem(Uri itemUri)
        {
            throw new NotImplementedException();
        }

        public Task<DataSample> RetrieveSample(Uri sampleUri)
        {
            throw new NotImplementedException();
        }

        public Task<Uri> StoreSample(DataSample sample)
        {
            throw new NotImplementedException();
        }

        public Task<Uri> UpdateSample(Uri sampleId, IEnumerable<DataItem> items, Func<DataSample, SampleSummary> onUpdate)
        {
            throw new NotImplementedException();
        }
    }
}
