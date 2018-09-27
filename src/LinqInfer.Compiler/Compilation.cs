using System;
using System.Collections.Generic;
using System.IO;
using LinqInfer.Utility.Expressions;

namespace LinqInfer.Compiler
{
    public class Compilation
    {
        readonly SourceCodeRepository _sourceCode;

        public Compilation(DirectoryInfo sourceDir)
        {
            _sourceCode = new SourceCodeRepository(sourceDir);
        }

        public Func<InvocationResult<object>> Compile(string[] args)
        {
            var main = _sourceCode.GetSourceCode("main");
            var parsedArgs = ParseArgs(args);

            return main.Code.AsFunc<object>(_sourceCode, p =>
            {
                if (parsedArgs.TryGetValue(p.Name, out var v))
                {
                    return Convert.ChangeType(v, p.Type);
                }

                return null;
            }, new ObjectParser());
        }

        static IDictionary<string, string> ParseArgs(string[] args)
        {
            var results = new Dictionary<string, string>();
            string lastKey = null;
            int i = 0;

            foreach (var arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    lastKey = arg;
                    continue;
                }

                results[lastKey?.Substring(1) ?? $"${i++}"] = arg;
            }

            return results;
        }
    }
}
