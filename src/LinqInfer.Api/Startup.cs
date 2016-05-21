using LinqInfer.Data.Sampling;
using LinqInfer.Data.Sampling.File;
using Microsoft.Owin;
using Owin;
using System.Web.Hosting;

[assembly: OwinStartup(typeof(LinqInfer.Api.Startup))]

namespace LinqInfer.Api
{
    public partial class Startup
    {
        public static ISampleStore Storage
        {
            get
            {
                return new FileStorageProvider(HostingEnvironment.MapPath("~/App_Data/storage"));
            }
        }

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            app.CreatePerOwinContext(() => Storage);
        }
    }
}
