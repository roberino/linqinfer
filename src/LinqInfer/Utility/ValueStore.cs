using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Utility
{
    class ValueStore
    {
        readonly object _lockObj;
        readonly Queue<double> _history;
        readonly int _maxHistory;
        double _total;
        long _counter;

        public ValueStore(int maxHistory)
        {
            _history = new Queue<double>(maxHistory);
            _maxHistory = maxHistory;
            _lockObj = new object();
        }

        public void Register(double value)
        {
            lock (_lockObj)
            {
                _total += value;
                _counter++;
                _history.Enqueue(value);

                if (_history.Count > _maxHistory)
                {
                    _history.Dequeue();
                }
            }
        }

        public double Average => _total / _counter;

        public long Count => _counter;

        public double? LastValue => _counter > 0 ? new double?(_history.Last()) : null;
        
        public double MovingError => _counter > 0 ? _history.Sum() / _counter : 0;

        public double Trend => _history.Count == _maxHistory ? _history.Last() - _history.Peek() : 0;
    }
}