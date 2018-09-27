using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Text
{
    public sealed class TokenisingTextWriter : TextWriter
    {
        readonly StringBuilder _buffer;
        readonly List<Func<IEnumerable<IToken>, IEnumerable<IToken>>> _filters;
        readonly List<Func<IEnumerable<IToken>, Task>> _sinks;
        readonly ITokeniser _tokeniser;
        readonly Encoding _encoding;
        readonly TextWriter _innerWriter;
        readonly bool _disposeInnerWriter;
        int _maxBufferSize;
        bool _isDisposed;
        int _charIndex;

        public TokenisingTextWriter(TextWriter innerWriter, bool disposeInnerWriter = false, ITokeniser tokeniser = null) : this(innerWriter.Encoding, tokeniser)
        {
            _disposeInnerWriter = disposeInnerWriter;
            _innerWriter = innerWriter;
        }

        public TokenisingTextWriter(Encoding encoding = null, ITokeniser tokeniser = null)
        {
            _maxBufferSize = 8192;
            _buffer = new StringBuilder();
            _filters = new List<Func<IEnumerable<IToken>, IEnumerable<IToken>>>();
            _sinks = new List<Func<IEnumerable<IToken>, Task>>();
            _encoding = encoding ?? Encoding.UTF8;
            _tokeniser = tokeniser ?? new Tokeniser();
        }

        public int MaxBufferSize
        {
            get
            {
                return _maxBufferSize;
            }
            set
            {
                Contract.Assert(value > 0);
                _maxBufferSize = value;
            }
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

        public override void Write(char value)
        {
            AssertNotDisposed();

            ProcessAsync(value).Wait();
        }

#if !NET_STD
        public override void Close()
        {
            _innerWriter?.Close();
        }
#endif

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

        async Task ProcessNumberAsync<T>(T number) where T : struct
        {
            await ProcessAsync(new[] { new Token(number.ToString(), 0, TokenType.Number) });
        }

        Task ProcessAsync(char ch)
        {
            return ProcessAsync(ch.ToString());
        }

        public async Task FlushTokenBuffer()
        {
            if(_buffer.Length > 0)
            {
                var text = _buffer.ToString();
                _buffer.Clear();
                await ProcessAsync(text, true);
            }
        }

        async Task ProcessAsync(string text, bool isLast = false)
        {
            if (_sinks.Any() && !string.IsNullOrEmpty(text))
            {
                var lastChar = text[text.Length - 1];

                if (char.IsWhiteSpace(lastChar) || isLast)
                {
                    string currentText;

                    if (_buffer.Length > 0)
                    {
                        _buffer.Append(text);
                        currentText = _buffer.ToString();
                    }
                    else
                    {
                        currentText = text;
                    }

                    var tokens = _tokeniser.Tokenise(currentText).ToList();

                    await ProcessAsync(tokens);
                }
                else
                {
                    _buffer.Append(text);

                    if(_buffer.Length > MaxBufferSize)
                    {
                        await TryFlushExcessBuffer();
                    }
                }
            }
        }

        async Task TryFlushExcessBuffer()
        {
            var tokens = _tokeniser.Tokenise(_buffer.ToString()).ToList();

            if (tokens.Count > 2)
            {
                _buffer.Clear();

                await ProcessAsync(tokens.Take(tokens.Count - 1));

                _buffer.Append(tokens.Last().Text);
            }


            if (_buffer.Length > MaxBufferSize)
            {
                throw new Exception("Buffer over max size: " + _buffer.Length);
            }
        }

        async Task ProcessAsync(IEnumerable<IToken> tokens)
        {
            if (_sinks.Any())
            {
                if (_charIndex > 0) tokens = Reindex(tokens).ToList();

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

        IEnumerable<IToken> Reindex(IEnumerable<IToken> tokens)
        {
            IToken last = null;

            foreach (var t in tokens)
            {
                if (t is Token)
                {
                    ((Token)t).Index = ((Token)t).Index + _charIndex;
                    last = t;
                    yield return t;
                }
                else
                {
                    var next = new Token(t.Text, t.Index + _charIndex, t.Type)
                    {
                        Weight = t.Weight
                    };

                    last = next;

                    yield return next;
                }
            }

            if (last != null)
            {
                _charIndex = last.Index + last.Text.Length;
            }
        }

        void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}