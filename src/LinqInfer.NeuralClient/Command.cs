using LinqInfer.Learning.Classification.Remoting;
using System;
using System.Threading.Tasks;

namespace LinqInfer.NeuralClient
{
    public class Command
    {
        private readonly Uri _serverEndpoint;

        public Command(Uri serverEndpoint)
        {
            _serverEndpoint = serverEndpoint;
        }

        protected Task InvokeClient(Func<IRemoteClassifierTrainingClient, Task> action)
        {
            using (var client = _serverEndpoint.CreateMultilayerNeuralNetworkClient())
            {
                return action(client);
            }
        }

        protected void InvokeClient(Action<IRemoteClassifierTrainingClient> action)
        {
            using (var client = _serverEndpoint.CreateMultilayerNeuralNetworkClient())
            {
                action(client);
            }
        }
    }
}