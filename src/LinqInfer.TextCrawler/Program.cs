using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.TextCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Run(options).Wait();
            }

            Console.ReadKey();
        }

        private static async Task Run(Options options)
        {
            var url = new Uri(options.Url);

            var engine = new CrawlerEngine();

            var index = await engine.CreateIndexAsync(url, new CancellationTokenSource(15000).Token);

            var doc = index.ExportAsXml();

            using (var fs = File.OpenWrite(options.OutputPath))
            {
                await doc.SaveAsync(fs, SaveOptions.OmitDuplicateNamespaces, CancellationToken.None);
            }
        }
    }
}