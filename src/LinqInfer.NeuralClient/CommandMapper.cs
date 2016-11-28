using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LinqInfer.NeuralClient
{
    public class CommandMapper
    {
        private IList<Type> _types;
        private TextWriter _output;

        public CommandMapper(TextWriter output)
        {
            _output = output;
            _types = typeof(Command).Assembly.ExportedTypes.Where(t => !t.IsAbstract && typeof(Command).IsAssignableFrom(t)).ToList();
        }

        public Func<Task> Map(string[] args)
        {
            var commandName = args[0].ToLower() + "command";
            var commandType = _types.FirstOrDefault(t => t.Name.ToLower() == commandName);

            if (commandType == null)
            {
                _output.WriteLine("Command not found: {0}", commandName);
                return () => Task.FromResult(0);
            }

            var commandInstance = commandType.GetConstructor(new[] { typeof(Uri) }).Invoke(new object[] { GetUri(args) });
            var executeMethod = commandType.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "Execute");
            var parameters = ParseArgs(executeMethod, args);

            return () =>
            {
                try
                {
                    if (typeof(Task).IsAssignableFrom(executeMethod.ReturnParameter.ParameterType))
                    {
                        return (Task)executeMethod.Invoke(commandInstance, parameters);
                    }

                    executeMethod.Invoke(commandInstance, parameters);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return Task.FromResult(0);
            };
        }

        private object ConvertArg(Type type, string value)
        {
            return Convert.ChangeType(value, type);
        }

        private object[] ParseArgs(MethodInfo method, string[] args)
        {
            return args.Skip(1).Zip(method.GetParameters(), (a, p) => ConvertArg(p.ParameterType, a)).ToArray();
            // return args.Skip(1).Select(a => (object)a).ToArray();
        }

        private Uri GetUri(string[] args)
        {
            return new Uri("tcp://localhost:9034");
        }
    }
}