using LinqInfer.Data.Serialisation;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Text;
using LinqInfer.Text.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.TextCrawler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                await Run(options);
            }

            Console.ReadKey();
        }

        static async Task Run(Options options)
        {
            if(options.Mode == "i")
            {
                await Index(options);
            }
            else
            {
                if (options.Mode == "n")
                {
                    await Network(options);
                }
                else
                {
                    await Extract(options);
                }
            }
        }

        static async Task Network(Options options)
        {
            const string fileName = "network-100.xml";
            const int size = 100;

            var nf = NetworkFactory<string>.CreateCategoricalNetworkFactory(size);

            INetworkClassifier<string, string> network;

            if (File.Exists(fileName))
            {
                using (var file = File.OpenText(fileName))
                {
                    var doc = XDocument.Load(file);
                    var pdoc = new PortableDataDocument(doc);
                    network = pdoc.OpenAsMultilayerNetworkClassifier<string, string>();
                }
            }
            else
            {
                network = nf.CreateLongShortTermMemoryNetwork<string>(size);
            }

            while (true)
            {
                var line = Console.ReadLine();

                if (string.IsNullOrEmpty(line))
                {
                    break;
                }

                var tokens = line.Tokenise();

                var words = tokens.Where(t => t.Type == TokenType.Word || t.Type == TokenType.SentenceEnd).ToList();

                if (line.EndsWith('?'))
                {
                    var lastResult = string.Empty;

                    foreach (var token in words)
                    {
                        var results = network.Classify(token.NormalForm());
                        lastResult = results.First().ClassType;
                        Console.Write(token.Text + ' ');
                    }

                    Console.Write(lastResult);
                    Console.WriteLine();
                }
                else
                {
                    var last = words.FirstOrDefault();

                    foreach (var token in words.Skip(1))
                    {
                        network.Train(last.NormalForm(), token.NormalForm());
                        last = token;
                    }

                    using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write))
                    {
                        network.ExportData().ExportAsXml().Save(file);
                    }
                }

                network.Reset();
            }

            await Task.CompletedTask;
        }

        static async Task Index(Options options)
        {
            var index = await new Uri(options.Url).CreateIndexAsync(new CancellationTokenSource(15000).Token);

            var doc = index.ExportAsXml();

            using (var fs = File.OpenWrite(options.OutputPath))
            {
                await doc.SaveAsync(fs, SaveOptions.OmitDuplicateNamespaces, CancellationToken.None);
            }
        }

        static async Task Extract(Options options)
        {
            var vectors = await new Uri(options.Url).ExtractVectorsAsync(
                new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token,
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
                await vectors.CreateCosineSimularityMatrix().WriteAsCsvAsync(writer);
            }
        }
    }
}