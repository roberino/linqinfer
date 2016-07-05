using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    internal class WordGraph
    {
        private const int MaxSize = 10000;

        private readonly IDictionary<Sequence<int>, int> _syntaxMap;
        private readonly IDictionary<int, Word> _words;

        public WordGraph()
        {
            _syntaxMap = new Dictionary<Sequence<int>, int>();
            _words = new Dictionary<int, Word>();
        }

        public void Analise(IEnumerable<IToken> wordStream)
        {
            const int size = 3;
            Sequence<int> current;
            var corpus = new Corpus(wordStream);

            int? nextId;
            int freq;

            foreach (var block in corpus.Blocks)
            {
                lock (_syntaxMap)
                {
                    var first = block.Select(w => GetWordId(w)).Where(w => w.HasValue).Take(size).Select(w => w.Value).ToArray();

                    if (first.Length < size) continue;

                    current = new Sequence<int>(first);
                }

                _syntaxMap[current] = 1;

                foreach (var word in block)
                {
                    lock (_syntaxMap)
                    {
                        nextId = GetWordId(word);

                        if (nextId.HasValue)
                        {
                            current = current.Permutate(nextId.Value);

                            freq = 0;

                            _syntaxMap.TryGetValue(current, out freq);

                            freq++;

                            _syntaxMap[current] = freq;

                            if (_syntaxMap.Count > (MaxSize * 1.5))
                            {
                                Purge();
                            }
                        }
                    }
                }
            }

            Purge();
        }

        public IEnumerable<Word> FindRelationships()
        {
            foreach (var g in _syntaxMap.GroupBy(s => new Tuple<int, int>(s.Key.First, s.Key.Last)))
            {
                foreach (var w in g)
                {
                    var wm = GetWord(w.Key.Last);

                    wm.AddRelationships(g.Select(x => new Relationship()
                    {
                        FormId = g.Key.GetHashCode(),
                        Target = GetWord(x.Key.Last),
                        Type = RelationshipType.SyntacticSubstitute,
                        Weight = 1
                    }));
                }
            }

            foreach (var w in _words.Values.OrderByDescending(w => w.Frequency))
            {
                foreach (var r in w.Relationships.OrderByDescending(x => x.Weight).Take(10))
                {
                    var spreadScore = _words.Values.Sum(v => v.RelationshipScore(r.Target));

                    r.Score = (float)Math.Log((double)spreadScore / w.RelationshipScore(r.Target));
                }

                if (w.Relationships.Any()) yield return w;
            }
        }

        private void Purge()
        {

        }

        private Word GetWord(int id)
        {
            Word word;

            _words.TryGetValue(id, out word);

            return word;
        }

        private int? GetWordId(IToken token)
        {
            var text = token.Text.ToLowerInvariant();
            var id = text.GetHashCode();

            Word word;

            if (!_words.TryGetValue(id, out word))
            {
                _words[id] = word = new Word(id, text);
            }

            word.Frequency++;

            return id;
        }
    }
}