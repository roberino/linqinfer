using System;
using LinqInfer.Text;
using System.Linq;
using LinqInfer.Learning;
using System.Threading.Tasks;

namespace LinqInfer.NeuralClient
{
    public class PushCommand : Command
    {
        public PushCommand(Uri serverEndpoint) : base(serverEndpoint) { }

        public Task Execute(string name, string text)
        {
            return InvokeClient(async c =>
            {
                var data = text.Split(',').Select(x => x.Split(':')).Select(x => new
                {
                    k = x[0],
                    v = x[1]
                })
                .AsQueryable();

                var pipeline = data.CreateTextFeaturePipeline();

                var trainingSet = pipeline.AsTrainingSet(x => x.k);

                await c.CreateClassifier(trainingSet, true, name);
            });
        }
    }
}
