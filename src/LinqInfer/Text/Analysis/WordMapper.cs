using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    internal class WordMapper
    {
        private const int MaxSize = 10000;

        private readonly IDictionary<Tuple<int, int, int>, int> _syntaxMap;

        public WordMapper()
        {
            _syntaxMap = new Dictionary<Tuple<int, int, int>, int>();
        }

        public void Process(IEnumerable<IToken> wordStream)
        {
            const int size = 3;

            var first = wordStream.Select(w => GetId(w)).Where(w => w.HasValue).Take(size).Select(w => w.Value).ToArray();

            var current = new Tuple<int, int, int>(first[0], first[1], first[2]);

            int? nextId;
            int freq;

            _syntaxMap[current] = 1;

            foreach (var word in wordStream.Skip(size))
            {
                nextId = GetId(word);

                if (nextId.HasValue)
                {
                    current = new Tuple<int, int, int>(current.Item2, current.Item3, nextId.Value);

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

            Purge();
        }

        public IEnumerable<WordMap> FindRelationships()
        {
            foreach (var g in _syntaxMap.GroupBy(s => new Tuple<int, int>(s.Key.Item1, s.Key.Item3)))
            {
                foreach (var w in g)
                {
                    var wm = new WordMap(w.Key.Item2);

                    wm.AddRange(g.Select(x => new Relationship()
                    {
                        Target = x.Key.Item2,
                        Type = RelationshipType.Syntactic
                    }));

                    yield return wm;
                }
            }
        }

        private void Purge()
        {
            
        }

        private int? GetId(IToken token)
        {
            return token.Text.GetHashCode();
        }
    }
}
