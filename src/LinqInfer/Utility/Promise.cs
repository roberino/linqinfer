using System;

namespace LinqInfer.Utility
{
    class Promise<TInput, TOutput>
    {
        readonly Func<TInput, TOutput> _func;
        readonly Func<TInput> _input;
        Func<TOutput, TOutput> _then;
        Func<Exception, TOutput> _error;

        public Promise(Func<TInput, TOutput> func, Func<TInput> input)
        {
            _func = func;
            _input = input;
        }

        public Promise<TInput, TOutput> Then(Func<TOutput, TOutput> then, Func<Exception, TOutput> error)
        {
            _then = then;
            _error = error;
            return this;
        }

        TOutput Execute()
        {
            try
            {
                var result = _func(_input());

                return _then == null ? result : _then(result);
            }
            catch (Exception ex)
            {
                if (_error != null)
                {
                    return _error(ex);
                }

                throw;
            }
        }
    }
}