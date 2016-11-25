using LinqInfer.Text;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.NeuralClient
{
    public class ClassifyCommand : Command
    {
        public ClassifyCommand(Uri serverEndpoint) : base(serverEndpoint) { }

        public async Task Execute(string name, string text)
        {
            await InvokeClient(async c =>
            {
                var data = new[]
                {
                    new
                    {
                        k = "?",
                        v = text
                    }
                }.AsQueryable();

                var pipeline = data.CreateTextFeaturePipeline();
                var classifierUri = c.CreateUri(name);

                var classifier = await c.RestoreClassifier(classifierUri, pipeline.FeatureExtractor, string.Empty);

                var results = classifier.Classify(data.First());

                foreach (var result in results)
                {
                    Console.WriteLine("{0}={1};", result.ClassType, result.Score);
                }
            });
        }
    }
}