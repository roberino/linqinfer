using LinqInfer.Data.Sampling;
using LinqInfer.Storage.SQLite.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Storage.SQLite.Providers
{
    public class SampleStore : StoreProvider, ISampleStore
    {
        public SampleStore(DirectoryInfo dataDir) : base(dataDir.FullName) { }

        public SampleStore(string dataDir = null) : base(dataDir) { }

        public async override Task Setup(bool reset = false)
        {
            await base.Setup(reset);

            await _db.CreateTableFor<SampleSummary>(!reset);
            await _db.CreateTableFor<DataSampleItem>(!reset);
            await _db.CreateTableFor<DataItemBlob>(!reset);
            await _db.CreateTableFor<SampleFieldMetadata>(!reset);
        }

        public async Task<DataSample> DeleteSample(Uri sampleUri)
        {
            var key = KeyFromUri(sampleUri);

            var sample = (await _db.QueryAsync<DataSampleItem>(x => x.Key == key)).SingleOrDefault();

            if (sample == null) throw MissingItemException(sampleUri);

            await _db.TransactAsync(async () =>
            {
                await _db.DeleteAsync<SampleSummary>(x => x.Id == sample.SummaryId);
                await _db.DeleteAsync<DataItemBlob>(x => x.SampleId == sample.Id);
                await _db.DeleteAsync<SampleFieldMetadata>(x => x.SampleId == sample.Id);
                await _db.DeleteAsync<DataSampleItem>(x => x.Id == sample.Id);
            });

            return sample;
        }

        public IQueryable<DataSampleHeader> ListSamples()
        {
            if (!_db.Exists<DataSampleItem>()) return Enumerable.Empty<DataSampleHeader>().AsQueryable();

            var samples = _db.Query<DataSampleItem>().ToList();

            foreach (var sample in samples)
            {
                var fields = _db.Query<SampleFieldMetadata>(x => x.SampleId == sample.Id);
                var summary = _db.Query<SampleSummary>(x => x.Id == sample.SummaryId).SingleOrDefault();

                if(summary != null)
                {
                    sample.Summary = summary;
                }

                sample.Metadata = new DataSampleMetadata()
                {
                    Fields = fields.Cast<FieldDescriptor>().ToList()
                };
            }

            return samples.AsQueryable();
        }

        public async Task<DataItem> RetrieveItem(Uri itemUri)
        {
            var key = KeyFromUri(itemUri);

            var item = (await _db.QueryAsync<DataItemBlob>(x => x.Key == key, 1)).SingleOrDefault();

            if (item == null) throw MissingItemException(itemUri);

            return item.Extract();
        }

        public async Task<DataSample> RetrieveSample(Uri sampleUri)
        {
            var key = KeyFromUri(sampleUri);

            var sampleHeader = (await _db.QueryAsync<DataSampleItem>(x => x.Key == key, 1)).SingleOrDefault();

            if (sampleHeader == null) throw MissingItemException(sampleUri);

            var summary = (await _db.QueryAsync<SampleSummary>(x => x.Id == sampleHeader.SummaryId)).SingleOrDefault();

            var items = (await _db.QueryAsync<DataItemBlob>(x => x.SampleId == sampleHeader.Id)).ToList();

            var fields = (await _db.QueryAsync<SampleFieldMetadata>(x => x.SampleId == sampleHeader.Id)).ToList();

            return new DataSample()
            {
                Key = sampleHeader.Key,
                Created = sampleHeader.Created,
                Id = sampleHeader.Id,
                Label = sampleHeader.Label,
                Modified = sampleHeader.Modified,
                Description = sampleHeader.Description,
                StartDate = sampleHeader.StartDate,
                EndDate = sampleHeader.EndDate,
                SourceDataUrl = sampleHeader.SourceDataUrl,
                Summary = summary,
                SampleData = items.Select(s => s.Extract()).ToList(),
                Metadata = new DataSampleMetadata()
                {
                    Fields = fields.Cast<FieldDescriptor>().ToList()
                }
            };
        }

        public async Task<Uri> StoreSample(DataSample sample)
        {
            sample.Recalculate();

            var header = new DataSampleItem()
            {
                Key = sample.Key,
                Created = sample.Created,
                Modified = sample.Modified,
                Label = sample.Label,
                SourceDataUrl = sample.SourceDataUrl,
                Description = sample.Description,
                StartDate = sample.StartDate,
                EndDate = sample.EndDate
            };

            await _db.TransactAsync(async () =>
            {
                var summaryId = await _db.InsertAsync(sample.Summary);

                header.SummaryId = summaryId.Value;

                var sampleId = await _db.InsertAsync(header);

                if (sample.SampleData.Any())
                {
                    var dataBlobs = sample.SampleData.Select(s => DataItemBlob.Create(s, sampleId.Value));

                    var count = await _db.InsertManyAsync(dataBlobs);
                }

                if (sample.Metadata != null && sample.Metadata.Fields.Any())
                {
                    var flds = sample.Metadata.Fields.Select(f => new SampleFieldMetadata()
                    {
                        DataType = f.DataType,
                        FieldUsage = f.FieldUsage,
                        Index = f.Index,
                        Label = f.Label,
                        Name = f.Name,
                        SampleId = sampleId.Value,
                        DataModel = f.DataModel
                    });

                    var count = await _db.InsertManyAsync(flds);
                }
            });

            sample.Id = header.Id;
            sample.Summary.Id = header.SummaryId;

            return header.Uri;
        }

        public Task<Uri> UpdateSample(Uri sampleId, IEnumerable<DataItem> items, Func<DataSample, SampleSummary> onUpdate)
        {
            throw new NotImplementedException();
        }

        private string KeyFromUri(Uri uri)
        {
            var key = uri.PathAndQuery.Split('/').Last().Trim();

            return key;
        }

        private Exception MissingItemException(Uri itemUri)
        {
            return new ArgumentException("Item missing - " + itemUri);
        }
    }
}
