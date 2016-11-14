﻿using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace LinqInfer.Text
{
    public class EnglishDictionary
    {
        private static readonly IDictionary<string, int> _words;

        static EnglishDictionary()
        {
            int i = 0;
            _words = ReadFile("en_dict.txt").ToDictionary(w => w, _ => i++);
        }

        /// <summary>
        /// Returns a enumeration of words.
        /// </summary>
        public IEnumerable<string> Words { get { return _words.Keys; } }

        /// <summary>
        /// Returns true for a word found within the dictionary.
        /// </summary>
        public bool IsWord(string word)
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

        private static HashSet<string> ReadFile(string name)
        {
            var data = new HashSet<string>();

            using (var stream = GetResource(name))
            {
                using (var reader = new StreamReader(stream))
                {
                    string line;

                    while (true)
                    {
                        line = reader.ReadLine();

                        if (line == null) break;

                        data.Add(line);
                    }
                }
            }

            return data;
        }

        private static Stream GetResource(string name)
        {
            var asm = typeof(EnglishDictionary).Assembly; // Assembly.GetExecutingAssembly();
            var names = asm.GetManifestResourceNames();
            var rname = names.FirstOrDefault(r => r.EndsWith(name));

            try
            {
                return asm.GetManifestResourceStream(rname);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("Can't load resource: {0} from {1}", name, string.Join(",", names)), ex);
            }
        }
    }
}