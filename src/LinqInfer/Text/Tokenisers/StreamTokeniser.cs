using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LinqInfer.Text.Tokenisers
{
    class StreamTokeniser
    {
        readonly Encoding _encoding;
        readonly TextReaderTokeniser _tokeniser;

        public StreamTokeniser(Encoding encoding = null, ITokeniser tokeniser = null)
        {
            _tokeniser = new TextReaderTokeniser(tokeniser);
            _encoding = encoding ?? Encoding.UTF8;
        }

        public IEnumerable<IToken> Tokenise(Stream stream)
        {
            using (var reader = new StreamReader(stream, _encoding, false, 1024, true))
            {
                return _tokeniser.Tokenise(reader);
            }
        }
    }
}