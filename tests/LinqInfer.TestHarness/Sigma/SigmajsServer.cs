using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Learning.MicroServices;
using LinqInfer.Maths;
using LinqInfer.Maths.Geometry;
using LinqInfer.Maths.Graphs;
using LinqInfer.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.TestHarness
{
    public static class SigmajsServer
    {
        public static void Run()
        {
            var serviceUrl = new Uri("http://localhost:8082");
            var uiUrl = new Uri("http://localhost:8080");

            using (SetupGraphService(serviceUrl, uiUrl))
            using (SetupFileServer(uiUrl))
            {
                Console.ReadLine();
            }
        }

        private static IDisposable SetupGraphService(Uri serviceUrl, Uri uiUrl)
        {
            var serviceApp = serviceUrl
                .CreateOwinApplication()
                .CreateGraphExportService(GenerateGraph, "/graphs/sofm/random")
                .AllowOrigin(uiUrl);

            serviceApp.AddErrorHandler((c, e) =>
            {
                c.Response.CreateTextResponse().WriteLine(e.Message);
                return Task.FromResult(false);
            });

            serviceApp.Start();

            Console.WriteLine("Listening at " + serviceUrl);

            return serviceApp;
        }

        private static IDisposable SetupFileServer(Uri uiUrl)
        {
            var physicalFileSystem = new PhysicalFileSystem((new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent).FullName);

            var options = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = physicalFileSystem
            };

            options.StaticFileOptions.FileSystem = physicalFileSystem;
            options.StaticFileOptions.ServeUnknownFileTypes = true;

            var server = (WebApp.Start(uiUrl.ToString(), app =>
            {
                app.UseFileServer(options);
            }));

            Console.WriteLine("Listening at " + uiUrl);

            return server;
        }

        private static async Task<WeightedGraph<string, double>> GenerateGraph(IOwinContext context, Rectangle rect)
        {
            var data = Enumerable.Range(1, 100).Select(n => Functions.RandomVector(2)).ToList().AsQueryable();
            var pipeline = data.CreatePipeline();
            
            var map = await pipeline.ToSofm(6, 0.2f, 0.1f, 1500).ExecuteAsync();

            map.ExportMode = GraphExportMode.Spatial3D;

            var graph = await map.ExportNetworkTopologyAsync(new VisualSettings(new Point3D() { X = rect.Width, Y = rect.Height, Z = (rect.Width + rect.Height) / 2 }));

            return graph;
        }
    }
}