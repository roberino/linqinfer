using System;

namespace LinqInfer.Utility.Expressions
{
    class DelegatingSourceCodeProvider : ISourceCodeProvider
    {
        readonly Func<string, SourceCode> _sourceCodeFunction;

        public DelegatingSourceCodeProvider(Func<string, SourceCode> sourceCodeFunction)
        {
            _sourceCodeFunction = sourceCodeFunction;
        }

        public DelegatingSourceCodeProvider(Func<string, string> sourceCodeFunction)
        {
            _sourceCodeFunction = n => SourceCode.Create(n, sourceCodeFunction(n));
        }

        public bool Exists(string name)
        {
            return (_sourceCodeFunction(name)?.Found).GetValueOrDefault();
        }

        public SourceCode GetSourceCode(string name)
        {
            return _sourceCodeFunction(name);
        }
    }
}