using System.Collections.Generic;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    public class ClassifierController : DataApiControllerBase
    {
        [Route("api/data/samples/{id}/classifiers")]
        public IEnumerable<string> GetClassifiers()
        {
            yield return "neural-network";
            yield return "naive-bayes";
        }
    }
}