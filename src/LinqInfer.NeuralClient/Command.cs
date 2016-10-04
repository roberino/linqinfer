using LinqInfer.Learning.Classification.Remoting;
using System;

namespace LinqInfer.NeuralClient
{
    internal class Command
    {
        private readonly Uri _serverEndpoint;

        public Command(Uri serverEndpoint)
        {
            _serverEndpoint = serverEndpoint;
        }

        public void Create()
        {
        }

        private void Execute(Action<IRemoteClassifierTrainingClient> action)
        {
            using (var client = _serverEndpoint.CreateMultilayerNeuralNetworkClient())
            {
                action(client);
            }
        }
    }
}