using LinqInfer.Data;
using LinqInfer.Data.Sampling;
using LinqInfer.Storage.SQLite.Providers;
using Microsoft.Owin;
using Owin;
using System.Web.Hosting;

[assembly: OwinStartup(typeof(LinqInfer.Api.Startup))]

namespace LinqInfer.Api
{
    public partial class Startup
    {
        public static ISampleStore SampleStore
        {
            get
            {
                //return new FileStorageProvider(HostingEnvironment.MapPath("~/App_Data/storage"));
                return new SampleStore(HostingEnvironment.MapPath("~/App_Data/storage"));
            }
        }
        public static IBlobStore BlobStore
        {
            get
            {
                return new BlobStore(HostingEnvironment.MapPath("~/App_Data/storage"));
            }
        }

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            
            app.CreatePerOwinContext(() => SampleStore);
            app.CreatePerOwinContext(() => BlobStore);
        }
    }
}
