using LinqInfer.Api.Models;
using LinqInfer.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    public class SampleStorageController : DataApiControllerBase
    {
        [Route("api/data/samples")]
        [HttpGet]
        public ResourceList<object> ListSamples()
        {
            int i = 1;

            return new ResourceList<object>(Storage.ListSamples().AsEnumerable().Select(s => new { name = "Sample " + i++, uri = ToConcreteUri(s), path = ToConcreteUri(s).PathAndQuery }));
        }

        [Route("api/data/samples")]
        public async Task<object> PostSample([FromBody] DataSample data)
        {
            if (data == null) throw new ArgumentNullException();

            data.Recalculate();

            var uri = await Storage.StoreSample(data);

            return new { uri = ToConcreteUri(uri), created = data.Created, summary = data.Summary };
        }

        [Route("api/data/samples/{id}")]
        public async Task<Resource<DataSample>> GetSample(string id)
        {
            var sample = await GetSampleById(id);

            var sampleResource = new Resource<DataSample>(sample, ToConcreteUri(sample.Uri));

            sampleResource.Views["self organising feature map"] = ToConcreteUri(sample.Uri, "/sofm");

            return sampleResource;
        }

        [Route("api/data/samples-csv")]
        public async Task<object> PostRawCsvSample([FromBody] string data)
        {
            if (data == null) throw new ArgumentNullException();

            var sample = new DataSample()
            {
                SampleData = new List<DataItem>()
            };

            foreach (var line in data.Split('\n', ']'))
            {
                var row = new DataItem();

                if (string.IsNullOrWhiteSpace(line)) continue;

                row.FeatureVector = line
                        .Split(',')
                        .Select(v => v.Trim())
                        .Where(v => v != "[" && !string.IsNullOrEmpty(v))
                        .Select(v => double.Parse(v))
                        .ToArray();

                sample.SampleData.Add(row);
            }

            return await PostSample(sample);
        }

        [Route("api/data/samples-naked")]
        public async Task<object> PostRawSample([FromBody] List<double[]> data)
        {
            if (data == null) throw new ArgumentNullException();

            var sample = new DataSample()
            {
                SampleData = new List<DataItem>()
            };

            foreach (var vect in data)
            {
                var row = new DataItem()
                {
                    FeatureVector = vect
                };

                sample.SampleData.Add(row);
            }

            return await PostSample(sample);
        }
    }
}
