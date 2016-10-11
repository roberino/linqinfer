using System;

namespace LinqInfer.NeuralClient
{
    public class EchoCommand : Command
    {
        public EchoCommand(Uri serverEndpoint) : base(serverEndpoint) { }

        public void Execute(string message)
        {
            Console.WriteLine(message);
        }
    }
}
