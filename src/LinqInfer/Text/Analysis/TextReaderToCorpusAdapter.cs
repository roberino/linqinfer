using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinqInfer.Text.Tokenisers;

namespace LinqInfer.Text.Analysis
{
    class TextReaderToCorpusAdapter
    {
        readonly ITokeniser _tokeniser;

        public TextReaderToCorpusAdapter(ITokeniser tokeniser = null)
        {
            _tokeniser = tokeniser ?? new Tokeniser();
        }

        public ICorpus CreateCorpus(TextReader reader)
        {
            return new VirtualCorpus(new EnumerableAdapter(reader, _tokeniser));
        }

        class EnumerableAdapter : IEnumerable<Task<IList<IToken>>>
        {
            readonly TextReader _reader;
            readonly ITokeniser _tokeniser;
            
            public EnumerableAdapter(TextReader reader, ITokeniser tokeniser)
            {
                _reader = reader;
                _tokeniser = tokeniser;
            }

            public IEnumerator<Task<IList<IToken>>> GetEnumerator()
            {
                return new Enumerator(_reader, _tokeniser);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(_reader, _tokeniser);
            }
        }

        class Enumerator : IEnumerator<Task<IList<IToken>>>
        {
            readonly TextReader _reader;
            readonly ITokeniser _tokeniser;

            Task _currentTask;
            IList<IToken> _currentData;
            bool _moveNext;
            bool _eof;
            int _indexOffset;

            public Enumerator(TextReader reader, ITokeniser tokeniser)
            {
                _reader = reader;
                _tokeniser = tokeniser;
            }

            public bool MoveNext()
            {
                if (!_eof)
                {
                    _moveNext = true;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public Task<IList<IToken>> Current => GetDataAsync();

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _eof = true;
            }

            async Task<IList<IToken>> GetDataAsync()
            {
                if (_moveNext)
                {
                    if (_currentTask != null && !_currentTask.IsCompleted)
                    {
                        throw new InvalidOperationException();
                    }

                    _currentTask = MoveNextAsync();

                    await _currentTask;
                }

                return _currentData;
            }

            async Task MoveNextAsync()
            {
                var nextLine = await _reader.ReadLineAsync();

                if (nextLine == null)
                {
                    _eof = true;
                    _moveNext = false;
                    _currentData = new List<IToken>();
                }
                else
                {
                    _currentData = _tokeniser.Tokenise(nextLine + Environment.NewLine, _indexOffset).ToList();

                    _indexOffset += nextLine.Length + 1;
                }
            }
        }
    }
}
