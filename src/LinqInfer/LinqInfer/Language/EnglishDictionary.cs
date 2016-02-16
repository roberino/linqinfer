using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LinqInfer
{
    public class EnglishDictionary
    {
        private static readonly HashSet<string> _words;
        private static readonly HashSet<string> _phonics;
        private static readonly List<Regex> _phonicsMapper;

        static EnglishDictionary()
        {
            _words = ReadFile("en_dict.txt");
            _phonics = ReadFile("en_phonics.txt");
            _phonicsMapper = _phonics.Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToList();
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
            var asm = Assembly.GetExecutingAssembly();
            var rname = asm.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(name));

            return asm.GetManifestResourceStream(rname);
        }

        public IEnumerable<string> Words { get { return _words; } }

        public IDictionary<string, int> PhonicMap(string word)
        {
            return _phonicsMapper.ToDictionary(p => p.ToString(), p => p.Matches(word).Count);
        }

        public bool IsWord(string word)
        {
            if (word == null) return false;

            return _words.Contains(word.ToLower());
        }

        public IEnumerable<string> FindWordsLike(string word)
        {
            throw new System.NotImplementedException();
        }
    }
}
