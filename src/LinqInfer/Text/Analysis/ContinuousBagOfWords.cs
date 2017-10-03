using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace LinqInfer.Text.Analysis
{
    public class ContinuousBagOfWords : IEnumerable<WordContext>
    {
        private readonly IEnumerable<IToken> _tokens;
        private readonly ISemanticSet _targetVocabulary;
        private readonly int _padding;

        public ContinuousBagOfWords(IEnumerable<IToken> tokens, ISemanticSet targetVocabulary, int paddingSize = 1)
        {
            Contract.Assert(paddingSize > 0);

            _tokens = tokens;
            _padding = paddingSize;
        }

        public IEnumerator<WordContext> GetEnumerator()
        {
            return Stream().GetEnumerator();
        }

        public IEnumerable<WordContext> Stream()
        {
            var bufferSize = _padding * 2 + 1;
            var buffer = new IToken[bufferSize];

            foreach (var token in _tokens)
            {
                if (IsFull(buffer))
                {
                    Dequeue(buffer);

                    var targ = buffer[_padding];

                    if (_targetVocabulary.IsDefined(targ.Text))
                    {
                        var context = new WordContext()
                        {
                            TargetWord = targ,
                            ContextualWords = Extract(buffer)
                        };

                        yield return context;
                    }
                }

                Enqueue(buffer, token);
            }
        }

        private IToken[] Extract(IToken[] buffer)
        {
            var context = new IToken[_padding * 2];
            var c = 0;

            for (var i = 0; i < buffer.Length; i++)
            {
                if(i != _padding)
                {
                    context[c++] = buffer[i];
                }
            }

            return context;
        }

        private bool IsFull(IToken[] buffer)
        {
            return buffer[buffer.Length - 1] != null;
        }

        private void Enqueue(IToken[] buffer, IToken token)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == null)
                {
                    buffer[i] = token;
                    break;
                }
            }
        }

        private void Dequeue(IToken[] buffer)
        {
            for (var i = 1; i < buffer.Length; i++)
            {
                buffer[i - 1] = buffer[i];
            }
            buffer[buffer.Length - 1] = null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}