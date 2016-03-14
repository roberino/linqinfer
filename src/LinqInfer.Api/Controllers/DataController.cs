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
    public class DataController : ApiController
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
            //storage://data/sample/c636f199-2957-4ec3-a859-ae25b540883c

            var sample = await Storage.RetrieveSample(new Uri("storage://data/sample/" + id));

            return sample;
        }

        private ISampleStorageProvider Storage
        {
            get
            {
                var ctx = Request.GetOwinContext();
                var store = ctx == null ? Startup.Storage : ctx.Get<ISampleStorageProvider>(typeof(ISampleStorageProvider).ToString());

                return store;
            }
        }
    }
}
