﻿using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Text.Analysis
{
    public sealed class Corpus : IBinaryPersistable, ICorpus
    {
        readonly IList<IToken> _tokens;

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

        public IAsyncEnumerator<IToken> ReadBlocksAsync()
        {
            return ReadBlocksAsyncInternal().AsAsyncEnumerator();
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
                return _tokens.Where(t => t.Type == TokenType.Word || t.Type == TokenType.Symbol);
            }
        }

        public IEnumerable<IEnumerable<IToken>> Blocks
        {
            get
            {
                return new BlockReader().ReadBlocks(_tokens);
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

        IEnumerable<Task<IList<IToken>>> ReadBlocksAsyncInternal()
        {
            foreach (var block in Blocks)
            {
                yield return Task.FromResult((IList<IToken>)block.ToList());
            }
        }
    }
}