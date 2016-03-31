using LinqInfer.Sampling;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    public class DataApiControllerBase : ApiController
    {
        protected async Task<DataSample> GetSampleById(string id)
        {
            //storage://data/sample/c636f199-2957-4ec3-a859-ae25b540883c

            var sample = await Storage.RetrieveSample(new Uri("storage://data/samples/" + id));

            return sample;
        }

        protected ISampleStorageProvider Storage
        {
            get
            {
                var ctx = Request.GetOwinContext();
                var store = ctx == null ? Startup.Storage : ctx.Get<ISampleStorageProvider>(typeof(ISampleStorageProvider).ToString());

                return store;
            }
        }

        protected Uri ToConcreteUri(Uri dataUri, string subView = null, string relPath = "/api/data")
        {
            return new Uri(Request.RequestUri, relPath + dataUri.PathAndQuery + subView);
        }
    }
}
