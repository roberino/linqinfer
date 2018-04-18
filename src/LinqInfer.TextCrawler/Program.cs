using LinqInfer.Text.Http;
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
            if(options.OutputPath == "i")
            {
                await Index(options);
            }
            else
            {
                await Extract(options);
            }
        }

        private static async Task Index(Options options)
        {
            var index = await new Uri(options.Url).CreateIndexAsync(new CancellationTokenSource(15000).Token);

            var doc = index.ExportAsXml();

            using (var fs = File.OpenWrite(options.OutputPath))
            {
                await doc.SaveAsync(fs, SaveOptions.OmitDuplicateNamespaces, CancellationToken.None);
            }
        }

        private static async Task Extract(Options options)
        {
            var vectors = await new Uri(options.Url).ExtractVectors(
                new CancellationTokenSource(1200000).Token,
                c => c.MaxNumberOfDocuments = 150,
                "good", "bad", "ugly", "pretty",
                "man", "woman", "king", "queen", "animal", "child", "goat",
                "clean", "dirty", "filthy", "pure", "female", "male", "big", "small",
                "strong", "weak", "health", "sick", "empire", "president",
                "pain", "pleasure", "boy", "girl", "hot", "cold", "white", "black",
                "big", "small");

            using (var fs = File.OpenWrite(options.OutputPath))
            using (var writer = new StreamWriter(fs))
            {
                await vectors.LabelledCosineSimularityMatrix.WriteAsCsvAsync(writer);
            }
        }
    }
}