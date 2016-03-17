using LinqInfer.Learning;
using LinqInfer.Probability;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    public class ClusterMapController : DataApiControllerBase
    {
        [Route("api/data/sample/{id}/cluster-map")]
        public async Task<object> GetClusterMap(string id, int nodeCount = 10, float learningRate = 0.5f)
        {
            var sample = await GetSampleById(id);

            var maxSample = sample.SampleData.Select(d => d.AsColumnVector()).MaxOfEachDimension().ToSingleArray();

            var sofm = sample.SampleData.AsQueryable().ToSofm(x => x == null ? maxSample : x.AsColumnVector().ToSingleArray(), nodeCount, learningRate);

            return new
            {
                map = sofm
            };
        }
    }
}
