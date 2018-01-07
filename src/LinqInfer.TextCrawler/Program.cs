using System;

namespace LinqInfer.TextCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                var url = new Uri(options.Url);


            }

            Console.WriteLine("Hello World!");
        }
    }
}