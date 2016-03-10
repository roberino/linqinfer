using LinqInfer.Learning;
using LinqInfer.Probability;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    }
}
