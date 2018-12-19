using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

            Task currentTask;
            IList<IToken> currentData;
            bool moveNext;
            bool eof;

            public Enumerator(TextReader reader, ITokeniser tokeniser)
            {
                _reader = reader;
                _tokeniser = tokeniser;
            }

            public bool MoveNext()
            {
                if (!eof)
                {
                    moveNext = true;
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
                eof = true;
            }

            async Task<IList<IToken>> GetDataAsync()
            {
                if (moveNext)
                {
                    if (currentTask != null && !currentTask.IsCompleted)
                    {
                        throw new InvalidOperationException();
                    }

                    currentTask = MoveNextAsync();

                    await currentTask;
                }

                return currentData;
            }

            async Task MoveNextAsync()
            {
                var nextLine = await _reader.ReadLineAsync();

                if (nextLine == null)
                {
                    eof = true;
                    moveNext = false;
                    currentData = new List<IToken>();
                }
                else
                {
                    currentData = _tokeniser.Tokenise(nextLine).ToList();
                }
            }
        }
    }
}
