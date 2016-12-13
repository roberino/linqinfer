using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    internal class DocumentIndex : IBinaryPersistable, IDocumentIndex, IXmlExportable, IXmlImportable
    {
        private readonly ITokeniser _tokeniser;
        private readonly IDictionary<string, TermDocumentFrequencyMap> _frequencies;

        private int _documentCount;

        public DocumentIndex(ITokeniser tokeniser = null)
        {
            _tokeniser = tokeniser ?? new Tokeniser();
            _frequencies = new Dictionary<string, TermDocumentFrequencyMap>();
        }

        public IFloatingPointFeatureExtractor<T> CreateVectorExtractor<T>(Func<T, IEnumerable<IToken>> tokeniser, int maxVectorSize = 128) where T : class
        {
            Contract.Requires(maxVectorSize > 0);

            var wf = WordFrequencies.ToList();

            return new VectorExtraction.TextVectorExtractor(wf
                .OrderByDescending(w => Math.Log((double)w.Item2 / (double)w.Item3 + 1))
                .Select(w => w.Item1)
                .Take(maxVectorSize), wf.Max(f => f.Item3)).CreateObjectTextVectoriser(tokeniser);
        }

        public IFloatingPointFeatureExtractor<T> CreateVectorExtractorByDocumentKey<T>(Func<T, IEnumerable<IToken>> tokeniser, int maxVectorSize = 128) where T : class
        {
            Contract.Requires(maxVectorSize > 0);

            var docKeys = DocumentKeys.ToList();
            var bucketSize = maxVectorSize / docKeys.Count;

            if (bucketSize < 1) throw new ArgumentException("Vector size too small");

            var frequentWordsByKey = new List<string>();

            int normFactor = 1;

            foreach (var key in docKeys)
            {
                var f = WordFrequenciesByDocumentKey(key).ToList();

                frequentWordsByKey.AddRange(f
                    .Select(x => new
                    {
                        term = x.Item1,
                        freqScore = Math.Log((double)x.Item3 / (double)x.Item2)
                    })
                    .OrderByDescending(
                        x => x.freqScore)
                        .Select(x => x.term).Take(bucketSize));

                var nf = f.Max(x => x.Item3);

                if (nf > normFactor) normFactor = nf;
            }

            return new VectorExtraction
                .TextVectorExtractor(frequentWordsByKey.Distinct(), normFactor)
                .CreateObjectTextVectoriser(tokeniser);
        }

        public IFloatingPointFeatureExtractor<IEnumerable<IToken>> CreateVectorExtractor(int maxVectorSize = 128, bool normalise = true)
        {
            Contract.Requires(maxVectorSize > 0);

            var wf = WordFrequencies.ToList();

            return new VectorExtraction.TextVectorExtractor(wf
                .OrderByDescending(w => Math.Log((double)w.Item2 / (double)w.Item3 + 1))
                .Select(w => w.Item1)
                .Take(maxVectorSize), normalise ? wf.Max(f => f.Item3) : 1, normalise);
        }

        public ITokeniser Tokeniser
        {
            get
            {
                return _tokeniser;
            }
        }

        public IEnumerable<string> Terms
        {
            get
            {
                return _frequencies.Keys;
            }
        }

        internal IEnumerable<string> DocumentKeys
        {
            get
            {
                return _frequencies.Values.SelectMany(v => v.DocFrequencies.Keys).Distinct();
            }
        }

        internal IEnumerable<Tuple<string, long, int>> WordFrequencies
        {
            get
            {
                return _frequencies.Select(f => new Tuple<string, long, int>(f.Key, f.Value.Count, f.Value.DocFrequencies.Count));
            }
        }

        internal IEnumerable<Tuple<string, long, int>> WordFrequenciesByDocumentKey(string docKey)
        {
            foreach (var freq in _frequencies.Where(f => f.Value.DocFrequencies.ContainsKey(docKey)))
            {
                var df = freq.Value.DocFrequencies[docKey];

                yield return new Tuple<string, long, int>(freq.Key, freq.Value.Count, df);
            }
        }

        public void IndexDocument(TokenisedTextDocument document)
        {
            bool indexedAlready = true;

            foreach (var wordGroup in document.Tokens.Where(t => t.Type == TokenType.Word).GroupBy(t => t.Text.ToLowerInvariant()))
            {
                TermDocumentFrequencyMap wordData;

                lock (_frequencies)
                {
                    if (!_frequencies.TryGetValue(wordGroup.Key, out wordData))
                    {
                        _frequencies[wordGroup.Key] = wordData = new TermDocumentFrequencyMap();
                    }
                }

                int tf;

                if (!wordData.DocFrequencies.TryGetValue(document.Id, out tf))
                {
                    wordData.Count++;
                    indexedAlready = false;
                }

                wordData.DocFrequencies[document.Id] = wordGroup.Count();
            }

            if (!indexedAlready)
            {
                _documentCount++;
            }
        }

        public void IndexText(string text, string id)
        {
            var doc = new TokenisedTextDocument(id, _tokeniser.Tokenise(text));
            IndexDocument(doc);
        }

        public void IndexDocuments(IEnumerable<TokenisedTextDocument> documents)
        {
            foreach (var doc in documents)
            {
                IndexDocument(doc);
            }
        }

        public void IndexDocuments(IEnumerable<KeyValuePair<string, XDocument>> documentKeyPairs)
        {
            var transformed = documentKeyPairs.Select(p => new TokenisedTextDocument(p.Key, ExtractWords(p.Value)));

            IndexDocuments(transformed.AsQueryable());
        }

        public void IndexDocuments(IEnumerable<XDocument> documents, Func<XDocument, string> keySelector)
        {
            IndexDocuments(documents.Select(d => new KeyValuePair<string, XDocument>(keySelector(d), d)));
        }

        public IEnumerable<TokenisedTextDocument> Tokenise(IEnumerable<XDocument> documents, Func<XDocument, string> keySelector)
        {
            return documents
                .Select(d => new KeyValuePair<string, XDocument>(keySelector(d), d))
                .Select(p => new TokenisedTextDocument(p.Key, ExtractWords(p.Value)));
        }

        public IEnumerable<SearchResult> Search(string query)
        {
            return SearchInternal(query).Select(r => new SearchResult()
            {
                ClassType = r.Key,
                Score = r.Value
            });
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
                TermDocumentFrequencyMap m;

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

        internal IEnumerable<KeyValuePair<string, float>> SearchInternal(string query)
        {
            var frequencyData = GetWordFrequencies(query);

            var docs = frequencyData.SelectMany(x => x.Value.DocFrequencies.Keys).Distinct().ToDictionary(x => x, x => 1d);

            foreach (var tf in frequencyData)
            {
                foreach (var doc in docs.Keys.ToList())
                {
                    int df = 0;

                    tf.Value.DocFrequencies.TryGetValue(doc, out df);

                    var idf = df > 0 ? Math.Log(df / (double)tf.Value.Count + 1) : 0d;

                    docs[doc] *= idf;
                }
            }

            return docs.Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .Select(d => new KeyValuePair<string, float>(d.Key, (float)d.Value));
        }

        private IDictionary<string, TermDocumentFrequencyMap> GetWordFrequencies(string text)
        {
            var words = _tokeniser.Tokenise(text).Where(t => t.Type == TokenType.Word);

            return words
                .Where(t => t.Type == TokenType.Word)
                .Distinct()
                .Select(w =>
                {
                    TermDocumentFrequencyMap m;

                    if (!_frequencies.TryGetValue(w.Text, out m))
                    {
                        m = new TermDocumentFrequencyMap() { Count = 0 };
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

        public XDocument ExportAsXml()
        {
            return new XDocument(
                new XElement("index", new XAttribute("doc-count", _documentCount),
                    _frequencies.Select(f =>
                        new XElement("term",
                            new XAttribute("text", f.Key),
                            new XAttribute("frequency", f.Value.Count),
                            f.Value
                                .DocFrequencies
                                .Select(d => new XElement("doc",
                                    new XAttribute("key", d.Key),
                                    new XAttribute("frequency", d.Value)))))));
        }

        public void ImportXml(XDocument xml)
        {
            _documentCount = int.Parse(xml.Root.Attribute("doc-count").Value);

            foreach (var element in xml.Root.Elements().Where(e => e.Name.LocalName == "term"))
            {
                _frequencies[element.Attribute("text").Value] = new TermDocumentFrequencyMap(element
                        .Elements()
                            .ToDictionary(e => e.Attribute("key").Value, e => int.Parse(e.Attribute("frequency").Value)),
                        long.Parse(element.Attribute("frequency").Value));
            }
        }

        private class TermDocumentFrequencyMap : IBinaryPersistable
        {
            private Dictionary<string, int> _docFrequencies;

            public TermDocumentFrequencyMap()
            {
                _docFrequencies = new Dictionary<string, int>();
                Count = 0;
            }

            public TermDocumentFrequencyMap(Dictionary<string, int> docFrequencies, long count)
            {
                _docFrequencies = docFrequencies;
                Count = count;
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