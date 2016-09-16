using LinqInfer.Learning.Classification.Remoting;
using LinqInfer.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            var trainer = new SemanicNetworkTrainer();
            ConsoleKeyInfo key;

            do
            {
                trainer.Run();

                key = Console.ReadKey();

            } while (key.Key != ConsoleKey.Escape);
        }
    }
}