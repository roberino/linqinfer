using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Storage
{
    public interface ISampleStorageProvider : IDisposable
    {
        Task<IEnumerable<Uri>> ListSamples();

        Task<Uri> StoreSample(DataSample sample);

        Task<Uri> UpdateSample(Uri sampleId, IEnumerable<DataItem> items, Func<DataSample, SampleSummary> onUpdate);

        Task<DataSample> RetrieveSample(Uri sampleUri);

        Task<DataSample> DeleteSample(Uri sampleUri);

        Task<DataItem> RetrieveItem(Uri itemUri);
    }
}
