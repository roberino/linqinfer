using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LinqInfer.Text
{
    internal class StreamTokeniser
    {
        private readonly ITokeniser _tokeniser;
        private readonly Encoding _encoding;

        public StreamTokeniser(Encoding encoding = null, ITokeniser tokeniser = null)
        {
            _tokeniser = tokeniser ?? new Tokeniser();
            _encoding = encoding ?? Encoding.UTF8;
        }

        public IEnumerable<IToken> Tokenise(Stream stream)
        {
            using (var reader = new StreamReader(stream, _encoding, false, 1024, true))
            {
                int index = 0;

                while (true)
                {
                    var line = reader.ReadLine();

                    if (line == null) break;

                    if (index != 0) yield return new Token("\n", index);

                    foreach (var token in _tokeniser.Tokenise(line)) yield return token;

                    index += line.Length;
                }
            }
        }
    }
}