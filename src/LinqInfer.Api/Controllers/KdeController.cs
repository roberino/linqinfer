using LinqInfer.Data.Sampling;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    public class KdeController : DataApiControllerBase
    {
        [Route("api/data/samples/{id}/kde")]
        public async Task<object> GetKde(string id, float bandwidth = 0.5f)
        {
            var sample = await GetSampleById(id);

            return sample.CreateMultiVariateDistribution();
        }
    }
}