﻿using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    internal class DocumentIndex : IBinaryPersistable
    {
        private readonly ITokeniser _tokeniser;
        private readonly IDictionary<string, WordMap> _frequencies;

        private int _documentCount;

        public DocumentIndex(ITokeniser tokeniser = null)
        {
            _tokeniser = tokeniser ?? new Tokeniser();
            _frequencies = new Dictionary<string, WordMap>();
        }

        public IFloatingPointFeatureExtractor<IEnumerable<IToken>> CreateVectorExtractor(int maxVectorSize = 128)
        {
            var wf = WordFrequencies.ToList();

            return new VectorExtraction.VectorExtractor(wf
                .OrderByDescending(w => Math.Log((double)w.Item2 / ((double)w.Item3) + 1))
                .Select(w => w.Item1)
                .Take(maxVectorSize), wf.Max(f => f.Item3));
        }

        public ITokeniser Tokeniser
        {
            get
            {
                return _tokeniser;
            }
        }

        internal IEnumerable<Tuple<string, long, int>> WordFrequencies
        {
            get
            {
                return _frequencies.Select(f => new Tuple<string, long, int>(f.Key, f.Value.Count, f.Value.DocFrequencies.Count));
            }
        }

        public void IndexDocuments(IQueryable<KeyValuePair<string, IEnumerable<IToken>>> documentKeyPairs)
        {
            foreach (var kp in documentKeyPairs)
            {
                _documentCount++;

                foreach (var word in kp.Value.Where(t => t.Type == TokenType.Word))
                {
                    WordMap wordData;

                    if (!_frequencies.TryGetValue(word.Text, out wordData))
                    {
                        wordData = new WordMap();
                    }
                    else
                    {
                        wordData.Count++;
                    }

                    int tf;

                    wordData.DocFrequencies.TryGetValue(kp.Key, out tf);

                    wordData.DocFrequencies[kp.Key] = tf + 1;

                    _frequencies[word.Text] = wordData;
                }
            }
        }

        public void IndexDocuments(IQueryable<KeyValuePair<string, XDocument>> documentKeyPairs)
        {
            var transformed = documentKeyPairs.Select(p => new KeyValuePair<string, IEnumerable<IToken>>(p.Key, ExtractWords(p.Value)));

            IndexDocuments(transformed.AsQueryable());
        }

        public void IndexDocuments(IQueryable<XDocument> documents, Func<XDocument, string> keySelector)
        {
            IndexDocuments(documents.Select(d => new KeyValuePair<string, XDocument>(keySelector(d), d)));
        }

        public IEnumerable<KeyValuePair<string, float>> Search(string query)
        {
            var results = new ConcurrentBag<KeyValuePair<XDocument, float>>();

            var idfs = GetWordFrequencies(query);

            var docs = idfs.SelectMany(x => x.Value.DocFrequencies.Keys).Distinct().ToDictionary(x => x, x => 1d);

            foreach (var df in idfs)
            {
                foreach (var doc in docs.Keys.ToList())
                {
                    int tf = 0;

                    df.Value.DocFrequencies.TryGetValue(doc, out tf);

                    var idf = tf > 0 ? Math.Log((double)df.Value.Count / ((double)tf) + 1) : 0d;

                    docs[doc] *= idf;
                }
            }

            return docs.Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .Select(d => new KeyValuePair<string, float>(d.Key, (float)d.Value));
        }

        public void Save(Stream output)
        {
            var sz = DictionarySerialiserFactory.ForInstance(_frequencies);

            sz.Write(_frequencies, output);
        }

        public void Load(Stream input)
        {
            var sz = DictionarySerialiserFactory.ForInstance(_frequencies);

            var data = sz.Read(input);

            foreach (var item in data)
            {
                WordMap m;

                if (!_frequencies.TryGetValue(item.Key, out m))
                {
                    _frequencies[item.Key] = item.Value;
                }
                else
                {
                    if (m.DocFrequencies.Keys.Intersect(item.Value.DocFrequencies.Keys).Any())
                    {
                        throw new ArgumentException("Duplicate keys found between the current index and new index data");
                    }

                    m.Count += item.Value.Count;

                    foreach (var k in item.Value.DocFrequencies) m.DocFrequencies.Add(k);
                }
            }
        }

        private IDictionary<string, WordMap> GetWordFrequencies(string text)
        {
            var words = _tokeniser.Tokenise(text).Where(t => t.Type == TokenType.Word);

            return words
                .Where(t => t.Type == TokenType.Word)
                .Distinct()
                .Select(w =>
            {
                WordMap m;

                if (!_frequencies.TryGetValue(w.Text, out m))
                {
                    m = new WordMap() { Count = 0 };
                }

                return new
                {
                    Word = w.Text,
                    Map = m
                };
            })
            .Where(f => f.Map.Count > 0)
            .ToDictionary(w => w.Word, v => v.Map);
        }

        private IEnumerable<IToken> ExtractWords(XDocument doc)
        {
            foreach (var node in AllText(doc.Root))
            {
                foreach (var word in _tokeniser.Tokenise(node.Value).Where(t => t.Type == TokenType.Word))
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

        private class WordMap : IBinaryPersistable
        {
            private Dictionary<string, int> _docFrequencies;

            public WordMap()
            {
                _docFrequencies = new Dictionary<string, int>();
                Count = 1;
            }

            public IDictionary<string, int> DocFrequencies { get { return _docFrequencies; } }

            public long Count { get; set; }

            public void Save(Stream output)
            {
                using (var writer = new BinaryWriter(output, Encoding.Default, true))
                {
                    writer.Write(Count);
                }

                var ds = DictionarySerialiserFactory.ForInstance(_docFrequencies);

                ds.Write(_docFrequencies, output);
            }

            public void Load(Stream input)
            {
                using (var reader = new BinaryReader(input, Encoding.Default, true))
                {
                    Count = reader.ReadInt64();
                }

                var ds = DictionarySerialiserFactory.ForInstance(_docFrequencies);

                foreach (var item in ds.Read(input))
                {
                    _docFrequencies[item.Key] = item.Value;
                }
            }
        }
    }
}