using LinqInfer.Learning;
using LinqInfer.Probability;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    public class SofmController : DataApiControllerBase
    {
        [Route("api/data/samples/{id}/sofm")]
        public async Task<object> GetClusterMap(string id, int nodeCount = 10, float learningRate = 0.5f)
        {
            var sample = await GetSampleById(id);

            var maxSample = sample.SampleData.Select(d => d.AsColumnVector()).MaxOfEachDimension().ToSingleArray();

            var sofm = sample.SampleData.AsQueryable().ToSofm(x => x == null ? maxSample : x.AsColumnVector().ToSingleArray(), nodeCount, learningRate);

            return new
            {
                features = sofm.FeatureLabels,
                map = sofm.Select(m => new
                {
                    weights = m.Weights,
                    numberOfMembers = m.GetMembers().Sum(x => x.Value),
                    euclideanLength = new ColumnVector1D(m.Weights).EuclideanLength
                })
            };
        }
    }
}
