﻿using LinqInfer.Utility;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    public class ContinuousBagOfWords
    {
        private readonly IEnumerable<IToken> _tokens;
        private readonly ISemanticSet _widerVocabulary;

        internal ContinuousBagOfWords(IEnumerable<IToken> tokens, ISemanticSet targetVocabulary, ISemanticSet widerVocabulary = null)
        {
            _tokens = ArgAssert.AssertNonNull(tokens, nameof(tokens));

            TargetVocabulary = ArgAssert.AssertNonNull(targetVocabulary, nameof(targetVocabulary));

            _widerVocabulary = widerVocabulary;
        }

        public ISemanticSet WiderVocabulary => _widerVocabulary ?? TargetVocabulary;

        public ISemanticSet TargetVocabulary { get; }

        public IEnumerable<BiGram> GetBiGrams(int padding = 2)
        {
            return GetNGrams(padding)
                .SelectMany(
                    c => c
                        .ContextualWords
                        .Select(
                            w => new BiGram(w.Text.ToLower(), c.TargetWord.Text.ToLower())));
        }

        public IEnumerable<SyntacticContext> GetNGrams(int padding = 2)
        {
            ArgAssert.AssertGreaterThanZero(padding, nameof(padding));

            return GetNGrams(_tokens, padding);
        }

        private IEnumerable<SyntacticContext> GetNGrams(IEnumerable<IToken> tokens, int padding)
        {
            var bufferSize = padding * 2 + 1;
            var buffer = new IToken[bufferSize];

            SyntacticContext nextContext = null;

            foreach (var token in tokens)
            {
                nextContext = MoveNext(buffer, padding);

                if (nextContext != null)
                {
                    yield return nextContext;

                    Dequeue(buffer);
                }

                Enqueue(buffer, token);
            }

            nextContext = MoveNext(buffer, padding);

            if (nextContext != null) yield return nextContext;
        }

        private SyntacticContext MoveNext(IToken[] buffer, int padding)
        {
            if (IsFull(buffer))
            {
                var targ = buffer[padding];

                if (TargetVocabulary.IsDefined(targ.Text.ToLowerInvariant()))
                {
                    var context = new SyntacticContext()
                    {
                        TargetWord = targ,
                        ContextualWords = Extract(buffer, padding)
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

        private IToken[] Extract(IToken[] buffer, int padding)
        {
            var context = new IToken[padding * 2];
            var c = 0;

            for (var i = 0; i < buffer.Length; i++)
            {
                if(i != padding)
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