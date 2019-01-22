using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LinqInfer.Text.Tokenisers
{
    class TextReaderTokeniser
    {
        readonly ITokeniser _tokeniser;
        readonly Encoding _encoding;

        public TextReaderTokeniser(ITokeniser tokeniser = null)
        {
            _tokeniser = tokeniser ?? new Tokeniser();
        }

        public IEnumerable<IToken> Tokenise(TextReader reader)
        {
            var indexOffset = 0;

            while (true)
            {
                var line = reader.ReadLine();

                if (line == null) break;
                
                foreach (var token in _tokeniser.Tokenise(line + Environment.NewLine, indexOffset)) yield return token;

                indexOffset += line.Length + 1;
            }
        }
    }
}