using LinqInfer.Data.Sampling;
using LinqInfer.Maths;
using LinqInfer.Storage.SQLite.Providers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Storage.SQLite.Tests.Providers
{
    [TestFixture]
    public class SampleStoreTests
    {
        [Test]
        public async Task StoreAsync_NoDataRows()
        {
            var sample = SetupData();

            sample.SampleData.Clear();

            using (var store = new SampleStore())
            {
                try
                {
                    await store.Setup(true);

                    await store.StoreSample(sample);
                }
                finally
                {
                    store.Destroy();
                }
            }
        }

        [Test]
        public async Task StoreAsync_ThenList()
        {
            var sample = SetupData();

            using (var store = new SampleStore())
            {
                try
                {
                    await store.Setup(true);

                    await store.StoreSample(sample);

                    var samples = store.ListSamples().ToList();

                    Assert.That(samples.Any(s => s.Id == sample.Id));
                }
                finally
                {
                    store.Destroy();
                }
            }
        }

        [Test]
        public async Task StoreAsync_ThenDelete()
        {
            var sample = SetupData();

            using (var store = new SampleStore())
            {
                try
                {
                    await store.Setup(true);

                    await store.StoreSample(sample);

                    await store.DeleteSample(sample.Uri);

                    var samples = store.ListSamples().ToList();

                    Assert.That(samples.Any(s => s.Id == sample.Id), Is.False);
                }
                finally
                {
                    store.Destroy();
                }
            }
        }

        [Test]
        public async Task StoreAsync_ExampleDataRows_ThenRetrieve()
        {
            var sample = SetupData();

            using (var store = new SampleStore())
            {
                try
                {
                    await store.Setup(true);

                    var uri = await store.StoreSample(sample);

                    var sample2 = await store.RetrieveSample(uri);

                    Assert.That(sample2, Is.Not.Null);

                }
                finally
                {
                    store.Destroy();
                }
            }
        }

        private DataSample SetupData()
        {
            var sample = new DataSample()
            {
                Created = DateTime.UtcNow,
                Description = "desc",
                Label = "label",
                Modified = DateTime.UtcNow,
                SampleData = Enumerable.Range(1, 10).Select(n => new DataItem() { FeatureVector = Functions.RandomVector(4).ToArray(), Label = n.ToString() }).ToList()
            };

            sample.Metadata.Fields.Add(new FieldDescriptor()
            {
                FieldUsage = FieldUsageType.Category,
                DataType = TypeCode.Boolean,
                Label = "Fieldx",
                Name = "FieldX"
            });

            return sample;
        }
    }
}