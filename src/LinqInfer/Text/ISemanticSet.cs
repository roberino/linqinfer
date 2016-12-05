using System.Collections.Generic;
using LinqInfer.Maths;
using System;
using LinqInfer.Data;

namespace LinqInfer.Text
{
    public interface ISemanticSet : ICloneableObject<ISemanticSet>
    {
        string this[int id] { get; }
        IEnumerable<string> Words { get; }
        IEnumerable<int> Encode(IEnumerable<string> tokens, bool appendUnknowns = false, Func<string, int> unknownValue = null);
        IEnumerable<string> Decode(IEnumerable<int> encodedIds, Func<int, string> unknownValue = null);
        IDictionary<string, Fraction> FindWordsLike(string word, float tolerance = 0.75F);
        int IdOf(string word);
        bool IsDefined(string word);
        int Append(string word);
    }
}