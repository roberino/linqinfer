using LinqInfer.Data.Sampling;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    public class DataApiControllerBase : ApiController
    {
        private readonly Lazy<ISampleStore> _store;

        public DataApiControllerBase()
        {
            _store = new Lazy<ISampleStore>(() =>
            {
                var ctx = Request.GetOwinContext();
                var store = ctx == null ? Startup.SampleStore : ctx.Get<ISampleStore>(typeof(ISampleStore).ToString());
                return store;
            });
        }

        protected async Task<DataSample> GetSampleById(string id)
        {
            //storage://data/sample/c636f199-2957-4ec3-a859-ae25b540883c

            var sample = await Storage.RetrieveSample(new Uri("storage://data/samples/" + id));

            return sample;
        }


        protected ISampleStore Storage
        {
            get
            {
                return _store.Value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _store.IsValueCreated)
            {
                _store.Value.Dispose();
            }

            base.Dispose(disposing);
        }

        protected Uri ToConcreteUri(Uri dataUri, string subView = null, string relPath = "/api/data")
        {
            return new Uri(Request.RequestUri, relPath + dataUri.PathAndQuery + subView);
        }
    }
}
