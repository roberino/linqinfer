using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    internal class TermFrequencyIdfDocumentSearch
    {
        private readonly ITokeniser _tokeniser;
        private readonly IDictionary<string, long> _frequencies;

        private int _documentCount;

        public TermFrequencyIdfDocumentSearch(ITokeniser tokeniser = null)
        {
            _tokeniser = tokeniser ?? new WordTokeniser();
            _frequencies = new Dictionary<string, long>();
        }

        public void IndexDocuments(IQueryable<XDocument> documents)
        {
            foreach (var doc in documents)
            {
                _documentCount++;

                foreach (var word in ExtractWords(doc))
                {
                    long c;

                    if (!_frequencies.TryGetValue(word, out c))
                    {
                        c = 1;
                    }
                    else
                    {
                        c++;
                    }
                    _frequencies[word] = c;
                }
            }
        }

        public IEnumerable<KeyValuePair<XDocument, float>> Search(IQueryable<XDocument> documents, string query)
        {
            var results = new ConcurrentBag<KeyValuePair<XDocument, float>>();

            var idfs = GetInverseDocumentFrequencies(query);

            documents.AsParallel().ForAll(d =>
            {
                var docWords = ExtractWords(d).Join(idfs, o => o, i => i.Key, (o, i) => i).ToList();

                if (docWords.Any())
                {
                    var f = docWords.Aggregate(1f, (t, p) => t * p.Value);

                    results.Add(new KeyValuePair<XDocument, float>(d, f));
                }
            });

            return results.OrderByDescending(r => r.Value);
        }

        public IDictionary<string, float> GetInverseDocumentFrequencies(string text)
        {
            var words = _tokeniser.Tokenise(text);

            return words.Select(w =>
            {
                long t;

                if (!_frequencies.TryGetValue(w, out t))
                {
                    t = 0;
                }

                return new
                {
                    Word = w,
                    Idf = (float)Math.Log(_documentCount / (double)(t + 1)) // idf  = log (N / df)
                };
            })
            .Where(f => f.Idf > 0)
            .ToDictionary(w => w.Word, v => v.Idf);
        }

        private IEnumerable<string> ExtractWords(XDocument doc)
        {
            foreach (var node in AllText(doc.Root))
            {
                foreach (var word in _tokeniser.Tokenise(node.Value))
                {
                    yield return word;
                }
            }
        }

        private IEnumerable<XText> AllText(XElement parent)
        {
            foreach (var node in parent.Nodes())
            {
                if (node.NodeType == System.Xml.XmlNodeType.Text)
                {
                    yield return (XText)node;
                }
                else
                {
                    if (node.NodeType == System.Xml.XmlNodeType.Element)
                    {
                        foreach (var text in AllText((XElement)node))
                        {
                            yield return text;
                        }
                    }
                }
            }
        }
    }
}