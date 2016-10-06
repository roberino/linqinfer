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

            var command = args[0];
        }

        static void PrintUsage()
        {

        }
    }
}
