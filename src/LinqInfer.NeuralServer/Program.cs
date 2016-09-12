using LinqInfer.Data;
using LinqInfer.Learning.Classification.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.NeuralServer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "trainer")
            {
                var endpoint = GetEndpoint(args);

                Console.WriteLine("Binding to endpoint " + endpoint);

                using (var fbs = new FileBlobStore())
                {
                    using (var server = endpoint.CreateClassifierTrainingServer(fbs))
                    {
                        server.Start();

                        Console.Read();
                    }
                }
            }
        }

        private static Uri GetEndpoint(string[] args)
        {
            var parameters = ParseArgs(args);
            string nextArg;
            int port;
            string path;
            string host;

            if (parameters.TryGetValue("port", out nextArg))
            {
                port = int.Parse(nextArg);
            }
            else
            {
                port = 9033;
            }

            if (!parameters.TryGetValue("host", out host))
            {
                host = "localhost";
            }

            if (!parameters.TryGetValue("path", out path))
            {
                path = "/";
            }

            return new Uri("tcp" + Uri.SchemeDelimiter + host + ":" + port + path);
        }

        private static IDictionary<string, string> ParseArgs(string[] args)
        {
            int i = 0;
            var dict = new Dictionary<string, string>();
            string nextKey = null;

            foreach(var a in args)
            {
                if (a.StartsWith("-"))
                {
                    if (nextKey != null)
                    {
                        dict[nextKey] = string.Empty;
                    }

                    nextKey = a.Substring(1);
                }
                else
                {
                    dict[nextKey ?? (i++).ToString()] = a;
                    nextKey = null;
                }
            }

            return dict;
        }
    }
}