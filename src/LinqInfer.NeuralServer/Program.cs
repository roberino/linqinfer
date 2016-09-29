using LinqInfer.Data;
using LinqInfer.Learning.Classification.Remoting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace LinqInfer.NeuralServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var endpoint = GetEndpoint(args);

            Console.WriteLine("Binding to endpoint " + endpoint);

            try
            {
                using (var fbs = new FileBlobStore())
                {
                    using (var server = endpoint.CreateMultilayerNeuralNetworkServer(fbs))
                    {
                        server.Start();

                        Console.Read();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Cant start server on address {0}", endpoint), ex);
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
                var envPortVar = GetConfigValue("PortEnvironmentVariable", string.Empty);
                port = string.IsNullOrEmpty(envPortVar) ? GetConfigValue("FallbackPort", 9012) : int.Parse(Environment.GetEnvironmentVariable(envPortVar));
            }

            if (!parameters.TryGetValue("host", out host))
            {
                var envHostVar = GetConfigValue("DnsEnvironmentVariable", string.Empty);
                host = string.IsNullOrEmpty(envHostVar) ? null : Environment.GetEnvironmentVariable(envHostVar);

                if (string.IsNullOrEmpty(host)) host = "localhost";
            }

            if (!parameters.TryGetValue("path", out path))
            {
                path = "/";
            }

            return new Uri("tcp" + Uri.SchemeDelimiter + host + ":" + port + path);
        }

        private static readonly AppSettingsReader _reader = new AppSettingsReader();

        private static T GetConfigValue<T>(string key, T defaultValue)
        {
            try
            {
                return (T)_reader.GetValue(key, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        private static IDictionary<string, string> ParseArgs(string[] args)
        {
            if (args.Length == 2 && args[1].All(c => char.IsLetterOrDigit(c)) && args[1].All(c => char.IsNumber(c)))
            {
                return new Dictionary<string, string>()
                {
                    { "host", args[0] },
                    { "port", args[1] }
                };
            }

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