using System;

namespace LinqInfer.Utility.Expressions
{
    class DelegatingSourceCodeProvider : ISourceCodeProvider
    {
        readonly Func<string, string> _sourceCodeFunction;

        public DelegatingSourceCodeProvider(Func<string, string> sourceCodeFunction)
        {
            _sourceCodeFunction = sourceCodeFunction;
        }

        public bool Exists(string name)
        {
            return _sourceCodeFunction(name) != null;
        }

        public string GetSourceCode(string name)
        {
            return _sourceCodeFunction(name);
        }
    }
}
