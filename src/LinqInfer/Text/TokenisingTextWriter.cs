using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Text
{
    public sealed class TokenisingTextWriter : TextWriter
    {
        private readonly List<Func<IEnumerable<IToken>, IEnumerable<IToken>>> _filters;
        private readonly List<Func<IEnumerable<IToken>, Task>> _sinks;
        private readonly ITokeniser _tokeniser;
        private readonly Encoding _encoding;
        private readonly TextWriter _innerWriter;
        private readonly bool _disposeInnerWriter;
        private bool _isDisposed;

        public TokenisingTextWriter(TextWriter innerWriter, bool disposeInnerWriter = false, ITokeniser tokeniser = null) : this(innerWriter.Encoding, tokeniser)
        {
            _disposeInnerWriter = disposeInnerWriter;
            _innerWriter = innerWriter;
        }

        public TokenisingTextWriter(Encoding encoding = null, ITokeniser tokeniser = null)
        {
            _filters = new List<Func<IEnumerable<IToken>, IEnumerable<IToken>>>();
            _sinks = new List<Func<IEnumerable<IToken>, Task>>();
            _encoding = encoding ?? Encoding.UTF8;
            _tokeniser = tokeniser ?? new Tokeniser();
        }

        public override Encoding Encoding
        {
            get
            {
                return _encoding;
            }
        }

        public void AddFilter(Func<IEnumerable<IToken>, IEnumerable<IToken>> filter)
        {
            AssertNotDisposed();
            _filters.Add(filter);
        }

        public void AddSink(Func<IEnumerable<IToken>, Task> outputSink)
        {
            AssertNotDisposed();
            _sinks.Add(outputSink);
        }

        public void AddSink(Action<IEnumerable<IToken>> outputSink)
        {
            AssertNotDisposed();
            _sinks.Add(t =>
            {
                outputSink(t);
                return Task.FromResult(true);
            });
        }

        public override void Write(char[] buffer, int index, int count)
        {
            AssertNotDisposed();
            _innerWriter?.Write(buffer, index, count);

            if (_sinks.Any())
            {
                ProcessAsync(new string(buffer, index, count)).Wait();
            }
        }

        public override void Write(decimal value)
        {
            AssertNotDisposed();
            _innerWriter?.Write(value);
            ProcessNumberAsync(value).Wait();
        }

        public override void Write(float value)
        {
            AssertNotDisposed();
            _innerWriter?.Write(value);
            ProcessNumberAsync(value).Wait();
        }

        public override void Write(long value)
        {
            AssertNotDisposed();
            _innerWriter?.Write(value);
            ProcessNumberAsync(value).Wait();
        }

        public override void Write(int value)
        {
            AssertNotDisposed();
            _innerWriter?.Write(value);
            ProcessNumberAsync(value).Wait();
        }

        public override void Write(double value)
        {
            AssertNotDisposed();
            _innerWriter?.Write(value);
            ProcessNumberAsync(value).Wait();
        }

        public override void Close()
        {
            _innerWriter?.Close();
        }

        public override void Flush()
        {
            AssertNotDisposed();

            _innerWriter?.Flush();
        }

        public override Task FlushAsync()
        {
            AssertNotDisposed();

            return _innerWriter?.FlushAsync();
        }

        public override async Task WriteAsync(char[] buffer, int index, int count)
        {
            var writeTask = _innerWriter?.WriteAsync(buffer, index, count);

            var outputTask = ProcessAsync(new string(buffer, index, count));

            await Task.WhenAll(writeTask, outputTask);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_disposeInnerWriter) _innerWriter?.Dispose();
                _sinks.Clear();
            }
            _isDisposed = true;
        }

        private async Task ProcessNumberAsync<T>(T number) where T : struct
        {
            await ProcessAsync(new[] { new Token(number.ToString(), 0, TokenType.Number) });
        }

        private async Task ProcessAsync(string text)
        {
            if (_sinks.Any())
            {
                var tokens = _tokeniser.Tokenise(text).ToList();

                await ProcessAsync(tokens);
            }
        }

        private async Task ProcessAsync(IEnumerable<IToken> tokens)
        {
            if (_sinks.Any())
            {
                foreach (var filter in _filters)
                {
                    tokens = filter(tokens);
                }

                var tasks = new List<Task>();

                foreach (var sink in _sinks)
                {
                    tasks.Add(sink(tokens));
                }

                await Task.WhenAll(tasks);
            }
        }

        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}