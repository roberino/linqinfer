using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    public sealed class ExecutionPipline<TResult>
    {
        private const int _errorThreshold = 15;
        private readonly Func<string, Task<TResult>> _execute;
        private readonly Func<bool, TResult, bool> _feedback;
        private readonly IList<Exception> _errors;
        private readonly Stopwatch _timer;

        internal ExecutionPipline(Func<string, TResult> execute, Func<bool, TResult, bool> feedback)
        {
            _execute = s => Task.FromResult(execute(s));
            _feedback = feedback;
            _errors = new List<Exception>();
            _timer = new Stopwatch();
        }

        internal ExecutionPipline(Func<string, Task<TResult>> execute, Func<bool, TResult, bool> feedback)
        {
            _execute = execute;
            _feedback = feedback;
            _errors = new List<Exception>();
            _timer = new Stopwatch();
        }

        public TimeSpan Elapsed { get { return _timer.Elapsed; } }

        public async Task<TResult> ExecuteAsync(string outputName = null)
        {
            try
            {
                _timer.Start();

                var res = await _execute(outputName);

                _feedback(true, res);

                _timer.Stop();

                return res;
            }
            catch (Exception ex)
            {
                _timer.Stop();
                _errors.Add(ex);
                throw;
            }
        }

        public TResult Execute(string outputName = null)
        {
            try
            {
                _timer.Start();

                var task = _execute(outputName);

                task.Wait();

                var res = task.Result;

                _feedback(true, res);

                _timer.Stop();

                return res;
            }
            catch (Exception ex)
            {
                _timer.Stop();
                _errors.Add(ex);
                throw;
            }
        }

        public TResult ExecuteUntil(Func<TResult, bool> condition, string outputName = null)
        {
            int errorStart = _errors.Count;

            while (true)
            {
                if (!_timer.IsRunning) _timer.Start();

                var nextTask = _execute(outputName);

                nextTask.Wait();

                var next = nextTask.Result;

                try
                {
                    if (condition(next))
                    {
                        if (_feedback(true, next))
                        {
                            _timer.Stop();

                            return next;
                        }
                    }
                    else
                    {
                        _feedback(false, next);
                    }
                }
                catch (Exception ex)
                {
                    _timer.Stop();

                    _errors.Add(ex);

                    if ((_errors.Count - errorStart) > _errorThreshold)
                    {
                        throw new AggregateException(_errors.Skip(errorStart));
                    }
                }
            }
        }

        public IEnumerable<Exception> Errors
        {
            get
            {
                return _errors;
            }
        }
    }
}