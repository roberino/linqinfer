using LinqInfer.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    public class ContinuousBagOfWords
    {
        private readonly IEnumerable<IToken> _tokens;
        private readonly ISemanticSet _targetVocabulary;
        private readonly ISemanticSet _widerVocabulary;
        private readonly int _padding;

        internal ContinuousBagOfWords(IEnumerable<IToken> tokens, ISemanticSet targetVocabulary, ISemanticSet widerVocabulary = null, int paddingSize = 1)
        {
            ArgAssert.AssertGreaterThanZero(paddingSize, nameof(paddingSize));

            _tokens = tokens;
            _targetVocabulary = targetVocabulary;
            _widerVocabulary = widerVocabulary;
            _padding = paddingSize;
        }

        public IEnumerable<BiGram> GetBiGrams()
        {
            return GetNGrams()
                .SelectMany(
                    c => c
                        .ContextualWords
                        .Select(
                            w => new BiGram(w.Text.ToLower(), c.TargetWord.Text.ToLower())));
        }

        public IEnumerable<SyntacticContext> GetNGrams()
        {
            return GetNGrams(_tokens);
        }

        private IEnumerable<SyntacticContext> GetNGrams(IEnumerable<IToken> tokens)
        {
            var bufferSize = _padding * 2 + 1;
            var buffer = new IToken[bufferSize];

            SyntacticContext nextContext = null;

            foreach (var token in tokens)
            {
                nextContext = MoveNext(buffer);

                if (nextContext != null)
                {
                    yield return nextContext;

                    Dequeue(buffer);
                }

                Enqueue(buffer, token);
            }

            nextContext = MoveNext(buffer);

            if (nextContext != null) yield return nextContext;
        }

        private SyntacticContext MoveNext(IToken[] buffer)
        {
            if (IsFull(buffer))
            {
                var targ = buffer[_padding];

                if (_targetVocabulary.IsDefined(targ.Text.ToLowerInvariant()))
                {
                    var context = new SyntacticContext()
                    {
                        TargetWord = targ,
                        ContextualWords = Extract(buffer)
                    };

                    if (_widerVocabulary != null)
                    {
                        context.ContextualWords = context.ContextualWords.Where(w => _widerVocabulary.IsDefined(w.Text.ToLowerInvariant())).ToArray();
                    }

                    return context;
                }
            }

            return null;
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
    }
}