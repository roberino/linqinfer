using System;

namespace LinqInfer.NeuralClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            var mapper = new CommandMapper(Console.Out);
            var action = mapper.Map(args);

            try
            {
                var task = action.Invoke();

                task.Wait();
            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.Message);
                foreach (var exi in ex.InnerExceptions)
                {
                    Console.WriteLine("- " + exi.Message);
#if DEBUG
                    Console.WriteLine("- " + exi.StackTrace);
#endif
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

#if DEBUG
            Console.ReadKey();
#endif
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: neuralclient <command> <args>");
        }
    }
}