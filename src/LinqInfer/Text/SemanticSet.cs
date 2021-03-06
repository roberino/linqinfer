﻿using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    public class SemanticSet : IImportableExportableSemanticSet
    {
        readonly IDictionary<string, int> _words;
        readonly Lazy<IDictionary<int, string>> _wordset;

        public SemanticSet(ISet<string> words)
        {
            int i = 0;
            _words = words.ToDictionary(w => w, _ => i++);
            _wordset = new Lazy<IDictionary<int, string>>(() => _words.ToDictionary(k => k.Value, v => v.Key));
        }

        public IEnumerable<int> Encode(IEnumerable<string> tokens, bool appendUnknowns = false, Func<string, int> unknownValue = null)
        {
            return tokens.Select(t =>
            {
                int id;

                if (_words.TryGetValue(t.ToLowerInvariant(), out id))
                {
                    return id;
                }

                if (appendUnknowns)
                {
                    return Append(t);
                }

                return unknownValue == null ? default(int) : unknownValue(t);
            });
        }

        public IEnumerable<string> Decode(IEnumerable<int> encodedIds, Func<int, string> unknownValue = null)
        {
            return encodedIds.Select(i =>
            {
                string val;

                if (_wordset.Value.TryGetValue(i, out val))
                {
                    return val;
                }

                return unknownValue == null ? null : unknownValue(i);
            });
        }

        public string RandomWord()
        {
            return this[Functions.Random(MaxId)];
        }

        /// <summary>
        /// Returns the last id
        /// </summary>
        public int MaxId { get { return _words.Any() ? _words.Max(w => w.Value) : -1; } }

        /// <summary>
        /// Returns the count of the words in the set
        /// </summary>
        public int Count { get { return _words.Count; } }

        /// <summary>
        /// Gets a word by internal id
        /// </summary>
        public string this[int id] { get { return _wordset.Value[id]; } }

        /// <summary>
        /// Returns a enumeration of words.
        /// </summary>
        public IEnumerable<string> Words { get { return _words.Keys; } }

        /// <summary>
        /// Returns true for a word found within the dictionary.
        /// </summary>
        public bool IsDefined(string word)
        {
            if (word == null) return false;

            return _words.ContainsKey(word.ToLower());
        }

        /// <summary>
        /// Returns the internal ID assigned to word or zero if the word isn't found.
        /// </summary>
        /// <param name="word">The word</param>
        /// <returns>An integer</returns>
        public int IdOf(string word)
        {
            int id = 0;
            if (word != null) _words.TryGetValue(word.ToLowerInvariant(), out id);
            return id;
        }

        /// <summary>
        /// Returns words which are statistically similar
        /// with regards to the number of edits required
        /// to transform from one word to another.
        /// </summary>
        /// <param name="word">The word</param>
        /// <param name="tolerance">The tolerance level as a percentage (between 0 and 1)</param>
        /// <returns>A dictionary of results and relevant scores</returns>
        public IDictionary<string, Fraction> FindWordsLike(string word, float tolerance = 0.75f)
        {
            Contract.Assert(word != null);
            Contract.Requires(tolerance > 0f && tolerance <= 1f);

            word = word.ToLower();

            return _words
                .Keys
                .Where(w => Math.Abs(w.Length - word.Length) < 3)
                .Select(w => new
                {
                    Word = w,
                    Diff = word.ComputeLevenshteinDifference(w)
                })
                .Where(x => x.Diff.Value >= tolerance)
                .ToDictionary(k => k.Word, v => v.Diff);
        }

        public int Append(string word)
        {
            if (word == null) throw new ArgumentNullException("word");

            lock (_words)
            {
                if (!_words.ContainsKey(word.ToLowerInvariant()))
                {
                    int id;
                    _words[word] = id = _words.Values.Max() + 1;
                    return id;
                }
            }

            throw new ArgumentException("Word already added to set");
        }

        public ISemanticSet Clone(bool deep)
        {
            return new SemanticSet(new HashSet<string>(_words.Keys));
        }

        public XDocument ExportAsXml()
        {
            return new XDocument(
                new XElement("set",
                    _words
                    .Select(w => new XElement("w", new XAttribute("text", w.Key), new XAttribute("id", w.Value)))));
        }

        public void ImportXml(XDocument xml)
        {
            foreach (var element in xml.Root.Elements().Where(e => e.Name.LocalName == "w"))
            {
                _words[element.Attribute("text").Value] = int.Parse(element.Attribute("id").Value);
            }
        }

        public static IImportableExportableSemanticSet FromXmlStream(Stream xml)
        {
            var sset = new SemanticSet(new HashSet<string>());
            var xmlDoc = XDocument.Load(xml);
            sset.ImportXml(xmlDoc);
            return sset;
        }

        public static implicit operator SemanticSet(string words)
        {
            var set = new HashSet<string>();

            foreach(var word in new Tokeniser().Tokenise(words))
            {
                if (!set.Contains(word.Text.ToLowerInvariant())) set.Add(word.Text.ToLowerInvariant());
            }

            return new SemanticSet(set);
        }
    }
}