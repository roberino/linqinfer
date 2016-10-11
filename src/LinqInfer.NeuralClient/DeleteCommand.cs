using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.NeuralClient
{
    public class DeleteCommand : Command
    {
        public DeleteCommand(Uri serverEndpoint) : base(serverEndpoint) { }

        public async Task Execute(string name)
        {
            await InvokeClient(async c =>
            {
                await c.Delete(c.CreateUri(name));
            });
        }
    }
}