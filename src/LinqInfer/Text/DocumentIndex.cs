﻿using LinqInfer.Data;
using LinqInfer.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            _tokeniser = tokeniser ?? new WordTokeniser();
            _frequencies = new Dictionary<string, WordMap>();
        }

        public void IndexDocuments(IQueryable<KeyValuePair<string, XDocument>> documentKeyPairs)
        {
            foreach (var kp in documentKeyPairs)
            {
                _documentCount++;

                foreach (var word in ExtractWords(kp.Value))
                {
                    WordMap wordData;

                    if (!_frequencies.TryGetValue(word, out wordData))
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

                    _frequencies[word] = wordData;
                }
            }
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
            var words = _tokeniser.Tokenise(text);

            return words
                .Distinct()
                .Select(w =>
            {
                WordMap m;

                if (!_frequencies.TryGetValue(w, out m))
                {
                    m = new WordMap() { Count = 0 };
                }

                return new
                {
                    Word = w,
                    Map = m
                };
            })
            .Where(f => f.Map.Count > 0)
            .ToDictionary(w => w.Word, v => v.Map);
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

        [Serializable]
        private class WordMap
        {
            private Dictionary<string, int> _docFrequencies;

            public WordMap()
            {
                _docFrequencies = new Dictionary<string, int>();
                Count = 1;
            }

            public IDictionary<string, int> DocFrequencies { get { return _docFrequencies; } }

            public long Count { get; set; }
        }
    }
}