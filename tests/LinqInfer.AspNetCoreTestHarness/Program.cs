using LinqInfer.AspNetCore;
using System;
using LinqInfer.Learning.MicroServices;
using LinqInfer.Learning;
using System.Threading.Tasks;
using LinqInfer.Maths.Graphs;
using System.Linq;
using LinqInfer.Maths.Geometry;
using LinqInfer.Maths;
using LinqInfer.Data.Remoting;

namespace LinqInfer.AspNetCoreTestHarness
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var endpoint = new Uri(args.Length > 0 ? args[0] : "http://localhost:8083");
            var api = endpoint.CreateHttpApi();

            api.Bind("/{p}").To(1, p => Task.FromResult(p * 5));
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
