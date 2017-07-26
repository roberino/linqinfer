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
    internal class DocumentIndex : IDocumentIndex
    {
        private readonly ITokeniser _tokeniser;
        private readonly IDictionary<string, TermDocumentFrequencyMap> _frequencies;
        private readonly Func<IList<DocumentTermWeightingData>, double> _calculationMethod;

        private int _documentCount;

        /// <summary>
        /// Creates a new document index using the default calculation method
        /// </summary>
        /// <param name="tokeniser">The tokeniser used to tokenise documents and queries</param>
        public DocumentIndex(ITokeniser tokeniser = null) : this(DocumentTermWeightingData.DefaultCalculationMethod, tokeniser)
        {
        }

        /// <summary>
        /// Creates a new document index
        /// </summary>
        /// <param name="weightCalculationMethod">The calculation method used to determine the weight of each result</param>
        /// <param name="tokeniser">The tokeniser used to tokenise documents and queries</param>
        public DocumentIndex(Func<IList<DocumentTermWeightingData>, double> weightCalculationMethod, ITokeniser tokeniser = null)
        {
            _tokeniser = tokeniser ?? new Tokeniser();
            _frequencies = new Dictionary<string, TermDocumentFrequencyMap>();
            _calculationMethod = weightCalculationMethod;
        }

        public long DocumentCount { get { return _documentCount; } }

        /// <summary>
        /// Gets the tokeniser used for parsing text
        /// </summary>
        public ITokeniser Tokeniser
        {
            get
            {
                return _tokeniser;
            }
        }

        /// <summary>
        /// Returns a list of terms referenced in the index
        /// </summary>
        public IEnumerable<string> Terms
        {
            get
            {
                return _frequencies.Keys;
            }
        }

        public IDictionary<string, long> GetTermFrequencies()
        {
            return _frequencies.ToDictionary(f => f.Key, f => (long)f.Value.DocFrequencies.Sum(d => d.Value));
        }

        /// <summary>
        /// Returns a set of terms as a <see cref="ISemanticSet"/>
        /// </summary>
        public IImportableExportableSemanticSet ExtractTerms()
        {
            return new SemanticSet(new HashSet<string>(_frequencies.Keys));
        }

        /// <summary>
        /// Returns a set of terms as a <see cref="ISemanticSet"/>
        /// </summary>
        public IImportableExportableSemanticSet ExtractKeyTerms(int maxNumberOfTerms)
        {
            Contract.Requires(maxNumberOfTerms > 0);

            var terms = _frequencies.Select(f => new DocumentTermWeightingData()
            {
                DocumentCount = _documentCount,
                DocumentFrequency = f.Value.Count,
                Term = f.Key,
                TermFrequency = f.Value.DocFrequencies.Sum(d => d.Value)
            })
            .Select(f => new { data = f, term = f.Term, score = DocumentTermWeightingData.DefaultCalculationMethodNoAdjust(new[] { f }) })
            .OrderByDescending(t => t.score)
            .Take(maxNumberOfTerms)
            .ToList();
            
            return new SemanticSet(new HashSet<string>(terms.Select(w => w.term)));
        }

        /// <summary>
        /// Returns an enumeration of <see cref="DocumentTermWeightingData"/> for a given document key
        /// </summary>
        internal IEnumerable<DocumentTermWeightingData> WordFrequenciesByDocumentKey(string docKey)
        {
            foreach (var freq in _frequencies.Where(f => f.Value.DocFrequencies.ContainsKey(docKey)))
            {
                var df = freq.Value.DocFrequencies[docKey];

                yield return new DocumentTermWeightingData() { Term = freq.Key, DocumentCount = _documentCount, DocumentFrequency = freq.Value.Count, TermFrequency = df };
            }
        }

        /// <summary>
        /// Indexes an individual document
        /// </summary>
        /// <param name="document">The document to be indexed</param>
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

        /// <summary>
        /// Indexes a single block of text as a document
        /// </summary>
        /// <param name="text">The text block</param>
        /// <param name="id">The id to associate with the text</param>
        public void IndexText(string text, string id)
        {
            var doc = new TokenisedTextDocument(id, _tokeniser.Tokenise(text));
            IndexDocument(doc);
        }

        /// <summary>
        /// Adds an enumeration of <see cref="TokenisedTextDocument"/> to the index
        /// </summary>
        /// <param name="documents">The documents to be added to the index</param>
        public void IndexDocuments(IEnumerable<TokenisedTextDocument> documents)
        {
            foreach (var doc in documents)
            {
                IndexDocument(doc);
            }
        }

        /// <summary>
        /// Adds an enumeration of <see cref="XDocument"/> to the index
        /// </summary>
        /// <param name="documentKeyPairs">The document and key pairs</param>
        public void IndexDocuments(IEnumerable<KeyValuePair<string, XDocument>> documentKeyPairs)
        {
            var transformed = documentKeyPairs.Select(p => new TokenisedTextDocument(p.Key, ExtractWords(p.Value)));

            IndexDocuments(transformed.AsQueryable());
        }

        /// <summary>
        /// Adds an enumeration of <see cref="XDocument"/> to the index
        /// </summary>
        /// <param name="documents">The documents to be added to the index</param>
        /// <param name="keySelector">A function which generates a unique key for a document</param>
        public void IndexDocuments(IEnumerable<XDocument> documents, Func<XDocument, string> keySelector)
        {
            IndexDocuments(documents.Select(d => new KeyValuePair<string, XDocument>(keySelector(d), d)));
        }

        /// <summary>
        /// Searches for documents containing terms within the query
        /// </summary>
        public IEnumerable<SearchResult> Search(string query)
        {
            return SearchInternal(query).Select(r => new SearchResult()
            {
                ClassType = r.Key,
                Score = r.Value
            });
        }

        /// <summary>
        /// Gets the raw weighting data for each term within a given query
        /// </summary>
        /// <param name="query">The query text</param>
        /// <returns>A dictionary of terms and <see cref="DocumentTermWeightingData"/></returns>
        public IDictionary<string, IList<DocumentTermWeightingData>> GetQueryWeightingData(string query)
        {
            var frequencyData = GetWordFrequencies(query);

            var docs = frequencyData.SelectMany(x => x.Value.DocFrequencies.Keys).Distinct().ToDictionary(x => x, x => (IList<DocumentTermWeightingData>)new List<DocumentTermWeightingData>());

            foreach (var termFreqData in frequencyData)
            {
                foreach (var doc in docs)
                {
                    int tf = 0;

                    termFreqData.Value.DocFrequencies.TryGetValue(doc.Key, out tf);

                    var data = new DocumentTermWeightingData()
                    {
                        Term = termFreqData.Key,
                        DocumentCount = _documentCount,
                        DocumentFrequency = termFreqData.Value.Count,
                        TermFrequency = tf
                    };

                    doc.Value.Add(data);
                }
            }

            return docs;
        }

        /// <summary>
        /// Saves the index data to the stream
        /// </summary>
        public void Save(Stream output)
        {
            var sz = DictionarySerialiserFactory.ForInstance(_frequencies);

            sz.Write(_frequencies, output);
        }

        /// <summary>
        /// Loads the index data from the stream
        /// </summary>
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

        /// <summary>
        /// Exports the index data as XML
        /// </summary>
        public XDocument ExportAsXml()
        {
            return new XDocument(
                new XElement("index", new XAttribute("doc-count", _documentCount),
                    _frequencies.Select(f =>
                        new XElement("term",
                            new XAttribute("text", f.Key),
                            new XAttribute("frequency", f.Value.Count),
                            KeyPairToString(f.Value.DocFrequencies)))));
        }

        /// <summary>
        /// Imports previously exported XML index data
        /// </summary>
        public void ImportXml(XDocument xml)
        {
            _documentCount = int.Parse(xml.Root.Attribute("doc-count").Value);

            foreach (var element in xml.Root.Elements().Where(e => e.Name.LocalName == "term"))
            {
                _frequencies[element.Attribute("text").Value] = new TermDocumentFrequencyMap(
                    StringToKeyPairs(element.Value).ToDictionary(k => k.Key, v => v.Value),
                        long.Parse(element.Attribute("frequency").Value));
            }
        }

        internal IEnumerable<string> DocumentKeys
        {
            get
            {
                return _frequencies.Values.SelectMany(v => v.DocFrequencies.Keys).Distinct();
            }
        }

        /// <summary>
        /// Returns an enumeration of <see cref="Tuple{T1, T2, T3}"/> 
        /// where Item1 = the term, Item2 = the term count over all docs and Item3 = the number of docs containing the term
        /// </summary>
        internal IEnumerable<Tuple<string, long, int>> WordFrequencies
        {
            get
            {
                return _frequencies.Select(f => new Tuple<string, long, int>(f.Key, f.Value.Count, f.Value.DocFrequencies.Count));
            }
        }

        /// <summary>
        /// Tokenises an enumeration of <see cref="XDocument"/> 
        /// </summary>
        /// <param name="documents">The documents to be tokenised</param>
        /// <param name="keySelector">A function which generates a unique key for a document</param>
        /// <returns></returns>
        internal IEnumerable<TokenisedTextDocument> Tokenise(IEnumerable<XDocument> documents, Func<XDocument, string> keySelector)
        {
            return documents
                .Select(d => new KeyValuePair<string, XDocument>(keySelector(d), d))
                .Select(p => new TokenisedTextDocument(p.Key, ExtractWords(p.Value)));
        }

        internal IEnumerable<KeyValuePair<string, float>> SearchInternal(string query)
        {
            var frequencyData = GetWordFrequencies(query);

            var weightings = GetQueryWeightingData(query);

            return weightings
                .Select(d => new
                {
                    key = d.Key,
                    score = _calculationMethod(d.Value)
                })
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .Select(d => new KeyValuePair<string, float>(d.key, (float)d.score));
        }

        internal IFloatingPointFeatureExtractor<T> CreateVectorExtractor<T>(Func<T, IEnumerable<IToken>> tokeniser, int maxVectorSize = 128) where T : class
        {
            Contract.Requires(maxVectorSize > 0);

            var wf = WordFrequencies.Select(w => new DocumentTermWeightingData()
                {
                    DocumentCount = _documentCount,
                    DocumentFrequency = w.Item2,
                    Term = w.Item1,
                    TermFrequency = w.Item3
                }).ToList();

            return new VectorExtraction.TextVectorExtractor(wf
                .OrderByDescending(w => _calculationMethod(new[] { w })) // Math.Log((double)w.Item2 / (double)w.Item3 + 1)
                .Select(w => w.Term)
                .Take(maxVectorSize), wf.Select(f => (int)f.TermFrequency).ToArray()).CreateObjectTextVectoriser(tokeniser);
        }

        internal IFloatingPointFeatureExtractor<T> CreateVectorExtractorByDocumentKey<T>(Func<T, IEnumerable<IToken>> tokeniser, int maxVectorSize = 128) where T : class
        {
            Contract.Requires(maxVectorSize > 0);

            var docKeys = DocumentKeys.ToList();
            var bucketSize = maxVectorSize / docKeys.Count;

            if (bucketSize < 1) throw new ArgumentException("Vector size too small");

            var frequentWordsByKey = docKeys.SelectMany(k =>
            {
                var f = WordFrequenciesByDocumentKey(k).ToList();

                return f
                    .Select(x => new
                    {
                        data = x,
                        freqScore = _calculationMethod(new[] { x }),
                        nf = f.Max(t => (int)t.TermFrequency)
                    })
                    .OrderByDescending(
                        x => x.freqScore)
                        .Select(x => x).Take(bucketSize);

            })
            .Distinct((x, y) => string.Equals(x.data.Term, y.data.Term), x => x.data.Term.GetHashCode())
            .ToList();

            return new VectorExtraction
                .TextVectorExtractor(frequentWordsByKey.Select(d => d.data.Term), frequentWordsByKey.Select(d => d.nf).ToArray())
                .CreateObjectTextVectoriser(tokeniser);
        }

        internal IFloatingPointFeatureExtractor<IEnumerable<IToken>> CreateVectorExtractor(int maxVectorSize = 128, bool normalise = true)
        {
            Contract.Requires(maxVectorSize > 0);

            var wf = WordFrequencies.ToList();

            return new VectorExtraction.TextVectorExtractor(wf
                .OrderByDescending(w => Math.Log((double)w.Item2 / (double)w.Item3 + 1))
                .Select(w => w.Item1)
                .Take(maxVectorSize), normalise ? wf.Max(f => f.Item3) : 1, normalise);
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

        private string KeyPairToString(IEnumerable<KeyValuePair<string, int>> pairs)
        {
            var sb = new StringBuilder();

            foreach(var kp in pairs)
            {
                sb.Append(kp.Key + ":" + kp.Value + ",");
            }

            return sb.ToString();
        }

        private IEnumerable<KeyValuePair<string, int>> StringToKeyPairs(string data)
        {
            foreach (var item in data.Split(','))
            {
                var i = item.LastIndexOf(':');

                if (i < 0) yield break;

                yield return new KeyValuePair<string, int>(item.Substring(0, i), int.Parse(item.Substring(i + 1)));
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
                using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
                {
                    writer.Write(Count);
                }

                var ds = DictionarySerialiserFactory.ForInstance(_docFrequencies);

                ds.Write(_docFrequencies, output);
            }

            public void Load(Stream input)
            {
                using (var reader = new BinaryReader(input, Encoding.UTF8, true))
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