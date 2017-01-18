using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Maths;
using LinqInfer.Utility;

namespace LinqInfer.Text
{
    public class EnglishDictionary : ISemanticSet
    {
        private static readonly ISemanticSet _baseSet;
        private readonly ISemanticSet _set;

        static EnglishDictionary()
        {
            _baseSet = new SemanticSet(ReadFile("en_dict.txt"));
        }

        public EnglishDictionary()
        {
            _set = _baseSet.Clone(true);
        }

        private EnglishDictionary(SemanticSet wordSet)
        {
            _set = wordSet;
        }

        public string this[int id]
        {
            get
            {
                return _set[id];
            }
        }

        public int Append(string word)
        {
            return _set.Append(word);
        }

        public IEnumerable<string> Words
        {
            get
            {
                return _set.Words;
            }
        }

        public string RandomWord()
        {
            return _set.RandomWord();
        }

        /// <summary>
        /// Returns the count of the words in the set
        /// </summary>
        public int Count { get { return _set.Count; } }

        public IDictionary<string, Fraction> FindWordsLike(string word, float tolerance = 0.75F)
        {
            return _set.FindWordsLike(word, tolerance);
        }

        public IEnumerable<int> Encode(IEnumerable<string> tokens, bool appendUnknown = false, Func<string, int> unknownValue = null)
        {
            return _set.Encode(tokens, appendUnknown, unknownValue);
        }

        public IEnumerable<string> Decode(IEnumerable<int> encodedIds, Func<int, string> unknownValue = null)
        {
            return _set.Decode(encodedIds, unknownValue);
        }

        public int IdOf(string word)
        {
            return _set.IdOf(word);
        }

        public bool IsDefined(string word)
        {
            return _set.IsDefined(word);
        }

        public bool IsWord(string word)
        {
            return _set.IsDefined(word);
        }

        public ISemanticSet Clone(bool deep)
        {
            return _set.Clone(deep);
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
            var asm = typeof(EnglishDictionary).GetTypeInf().Assembly; // Assembly.GetExecutingAssembly();
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