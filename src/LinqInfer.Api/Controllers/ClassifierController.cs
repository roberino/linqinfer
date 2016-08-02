using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using LinqInfer.Data.Sampling;
using LinqInfer.Learning;

namespace LinqInfer.Api.Controllers
{
    public class ClassifierController : DataApiControllerBase
    {
        [Route("api/data/samples/{id}/classifiers")]
        public IEnumerable<Uri> GetClassifiers()
        {
            yield return RelativeApiUri("neural-network");
            yield return RelativeApiUri("naive-bayes");
        }

        [Route("api/data/samples/{id}/classifiers/neural-network")]
        public async Task<object> CreateNNClassifier(string id, float errorTolerance = 0.3f)
        {
            var sample = await GetSampleById(id);

            var classifier = sample
                .CreatePipeline()
                .OutputResultsTo(Blobs)
                .ToMultilayerNetworkClassifier(x => x.Label, errorTolerance)
                .Execute(id);

            return new
            {
                success = true
            };
        }
    }
}