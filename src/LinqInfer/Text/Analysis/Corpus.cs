using LinqInfer.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    internal sealed class Corpus : IBinaryPersistable
    {
        private readonly IList<IToken> _tokens;

        public Corpus()
        {
            _tokens = new List<IToken>();
        }

        public Corpus(IEnumerable<IToken> tokens)
        {
            _tokens = tokens.ToList();
        }

        public void Append(IToken token)
        {
            _tokens.Add(token);
        }

        public IEnumerable<IToken> Words
        {
            get
            {
                return _tokens.Where(t => t.Type == TokenType.Word);
            }
        }

        public IEnumerable<IToken> WordsAndSymbols
        {
            get
            {
                return _tokens.Where(t => t.Type == TokenType.Word);
            }
        }

        public IEnumerable<IEnumerable<IToken>> Blocks
        {
            get
            {
                var currentBlock = new List<IToken>();
                int spaceCount = 0;
                int i = 0;
                TokenType lastType = TokenType.Null;

                foreach(var word in _tokens)
                {
                    if (word.Type == TokenType.Word || word.Type == TokenType.Number)
                    {
                        currentBlock.Add(new Token(word.Text, i++, TokenType.Word));
                        spaceCount = 0;
                    }
                    else
                    {
                        if (word.Type == TokenType.SentenceEnd || spaceCount > 1 || ((lastType == TokenType.Word || lastType == TokenType.Number) && word.Type == TokenType.Symbol && (word.Text == ";" || word.Text == ",")))
                        {
                            yield return currentBlock;

                            currentBlock.Clear();
                            spaceCount = 0;
                        }
                        else
                        {
                            if(word.Type == TokenType.Space)
                            {
                                spaceCount++;
                            }
                        }
                    }

                    lastType = word.Type;
                }

                if (currentBlock.Any()) yield return currentBlock;
            }
        }

        public void Load(Stream input)
        {
            using (var reader = new StreamReader(input))
            {
                int i = 0;
                while (true)
                {
                    var next = reader.ReadLine();

                    if (next == null) break;

                    var type = next == "." ? TokenType.SentenceEnd : TokenType.Word;

                    _tokens.Add(new Token(next, i++, type));
                }
            }
        }

        public void Save(Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                foreach (var block in Blocks)
                {
                    foreach (var word in block)
                    {
                        writer.WriteLine(word.Text);
                    }

                    writer.WriteLine(".");
                }
            }
        }
    }
}