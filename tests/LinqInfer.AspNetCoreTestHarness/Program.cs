using LinqInfer.AspNetCore;
using LinqInfer.AspNetCoreTestHarness.Text;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Learning.MicroServices;
using LinqInfer.Maths;
using LinqInfer.Maths.Geometry;
using LinqInfer.Maths.Graphs;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.AspNetCoreTestHarness
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var endpoint = new Uri(args.Length > 0 ? args[0] : "http://0.0.0.0:8083");
            var api = endpoint.CreateHttpApi();
            var sz = new JsonObjectSerialiser();

            new TextServices(api);

            api.AddErrorHandler(async (c, e) =>
            {
                if (e is FileNotFoundException)
                {
                    c.Response.CreateStatusResponse(404);
                }
                else
                {
                    c.Response.CreateStatusResponse(500);
                }

                await sz.Serialise(new { error = e.GetType().Name, message = e.Message, stack = e.StackTrace }, Encoding.UTF8, sz.SupportedMimeTypes.First(), c.Response.Content);
                
                return true;
            });

            api.CreateGraphExportService(GenerateGraph, "/graph");

            using (api)
            {
                api.Start();
            }
        }

        private static async Task<WeightedGraph<string, double>> GenerateGraph(IOwinContext context, Rectangle rect)
        {
            var data = Enumerable.Range(1, 100).Select(n => Functions.RandomVector(2)).ToList().AsQueryable();
            var pipeline = data.CreatePipeline();

            var map = await pipeline.ToSofm(5, 0.2f, 0.1f, 1500).ExecuteAsync();

            map.ExportMode = GraphExportMode.Spatial3D;

            var graph = await map.ExportNetworkTopologyAsync(new VisualSettings(new Point3D() { X = rect.Width, Y = rect.Height, Z = (rect.Width + rect.Height) / 2 }));

            return graph;
        }
    }
}
