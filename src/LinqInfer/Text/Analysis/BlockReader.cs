using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    internal class BlockReader
    {
        /// <summary>
        /// Reads tokens, taking only words and numbers
        /// and splitting them based on white space and sentence endings.
        /// </summary>
        public IEnumerable<IEnumerable<IToken>> ReadBlocks(IEnumerable<IToken> tokens)
        {
            var currentBlock = new List<IToken>();
            int spaceCount = 0;
            int i = 0;
            TokenType lastType = TokenType.Null;

            foreach (var word in tokens)
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
                        if (currentBlock.Any())
                        {
                            yield return currentBlock.ToList();

                            currentBlock.Clear();
                        }
                        spaceCount = 0;
                    }
                    else
                    {
                        if (word.Type == TokenType.Space)
                        {
                            spaceCount++;
                        }
                    }
                }

                lastType = word.Type;
            }

            if (currentBlock.Any()) yield return currentBlock.ToList();
        }
    }
}
