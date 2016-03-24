using LinqInfer.Probability;
using LinqInfer.Storage;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    public class SofmController : DataApiControllerBase
    {
        [Route("api/data/samples/{id}/sofm")]
        public async Task<object> GetSofm(string id, int nodeCount = 10, float learningRate = 0.5f)
        {
            var sample = await GetSampleById(id);           

            var sofm = sample.CreateSofm();

            return new
            {
                metadata = sample.Metadata,
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
