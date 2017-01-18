using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data.Sampling
{
    public interface ISampleStore : IDisposable
    {
        IQueryable<DataSampleHeader> ListSamples();

        Task<Uri> StoreSample(DataSample sample);

        Task<DataSample> RetrieveSample(Uri sampleUri);

        Task<DataSample> DeleteSample(Uri sampleUri);

        Task<DataItem> RetrieveItem(Uri itemUri);
    }
}
