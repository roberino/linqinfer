using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace LinqInfer.Maths.Probability
{
    public class MonteCarloSimulation
    {
        private readonly Func<ColumnVector1D, bool> _qualifyingFunc;
        private readonly Func<Fraction, Fraction> _outputFunc;
        private readonly int _vectorSize;

        private int _maxIterations;

        public MonteCarloSimulation(Func<ColumnVector1D, bool> qualifyingFunc, int vectorSize = 2, Func<Fraction, Fraction> outputFunc = null)
        {
            Contract.Assert(qualifyingFunc != null);
            Contract.Requires(vectorSize > 0);

            _qualifyingFunc = qualifyingFunc;
            _outputFunc = outputFunc ?? (x => x);
            _vectorSize = vectorSize;

            ParallelProcess = true;
            Elapsed = TimeSpan.Zero;
        }

        public bool ParallelProcess { get; set; }

        public Fraction Simulate(int iterations = 10000)
        {
            Contract.Requires(iterations > 0);

            int hits = 0;
            var pp = ParallelProcess;

            var sw = new Stopwatch();

            sw.Start();

            if (pp)
            {
                Enumerable.Range(0, iterations)
                    .AsParallel()
                    .ForAll(n =>
                    {
                        var rnd = Functions.RandomVector(_vectorSize);

                        if (_qualifyingFunc(rnd))
                            Interlocked.Increment(ref hits);
                    });
            }
            else
            {
                for (var i = 0; i < iterations; i++)
                {
                    var rnd = Functions.RandomVector(_vectorSize);

                    if (_qualifyingFunc(rnd)) hits++;
                }
            }

            var est = _outputFunc(new Fraction(hits, iterations));

            sw.Stop();

            lock (_outputFunc)
            {
                CurrentEstimate = est;
                Elapsed += sw.Elapsed;

                if (iterations > _maxIterations)
                {
                    _maxIterations = iterations;
                    BestEstimate = est;
                }
            }

            return est;
        }

        public void Reset()
        {
            Elapsed = TimeSpan.Zero;
            CurrentEstimate = null;
            BestEstimate = null;
        }

        public TimeSpan Elapsed { get; private set; }

        public Fraction? CurrentEstimate { get; private set; }

        public Fraction? BestEstimate { get; private set; }

        public override string ToString()
        {
            return string.Format("Current={0}, Best={1}, Elapsed={2}, Parallel={3}", CurrentEstimate, BestEstimate, Elapsed, ParallelProcess);
        }
    }
}