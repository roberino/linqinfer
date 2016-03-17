using LinqInfer.Learning;
using LinqInfer.Probability;
using LinqInfer.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    public class SampleStorageController : DataApiControllerBase
    {
        [Route("api/data/summary")]
        public object GetSummary(string sample = null)
        {
            var data = sample.Split(',').Select(c => double.Parse(c)).ToList();
            var muStdDev = Functions.MeanStdDev(data);
            var sum = data.Sum();
            var min = data.Min();
            var max = data.Max();

            return new
            {
                mean = muStdDev.Item1,
                stdDev = muStdDev.Item2,
                min = min,
                max = max,
                sum = sum
            };
        }
        [Route("api/data/sample-csv")]
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

        [Route("api/data/sample-naked")]
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

                //row.FeatureVector = line
                //        .Split(',')
                //        .Select(v => v.Trim())
                //        .Where(v => v != "[" && !string.IsNullOrEmpty(v))
                //        .Select(v => double.Parse(v))
                //        .ToArray();

                sample.SampleData.Add(row);
            }

            return await PostSample(sample);
        }

        [Route("api/data/sample")]
        public async Task<object> PostSample([FromBody] DataSample data)
        {
            if (data == null) throw new ArgumentNullException();

            data.Recalculate();

            var uri = await Storage.StoreSample(data);

            return new { id = data.Id, uri = uri, created = data.Created };
        }

        [Route("api/data/sample/{id}")]
        public async Task<DataSample> GetSample(string id)
        {
            return await GetSampleById(id);
        }
    }
}
