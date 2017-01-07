using System.Collections.Generic;
using LinqInfer.Maths;
using System;
using LinqInfer.Data;

namespace LinqInfer.Text
{
    /// <summary>
    /// Represents a set of semantic text tokens (i.e. words)
    /// </summary>
    public interface ISemanticSet : ICloneableObject<ISemanticSet>
    {
        /// <summary>
        /// Gets a word by ID
        /// </summary>
        string this[int id] { get; }

        /// <summary>
        /// Returns a enumeration of all words
        /// </summary>
        IEnumerable<string> Words { get; }

        /// <summary>
        /// Returns an enumeration of words encoded as ids
        /// </summary>
        /// <returns></returns>
        IEnumerable<int> Encode(IEnumerable<string> tokens, bool appendUnknowns = false, Func<string, int> unknownValue = null);

        /// <summary>
        /// Returns an enumeration of words decoded by ID
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> Decode(IEnumerable<int> encodedIds, Func<int, string> unknownValue = null);

        /// <summary>
        /// Finds words which are statistically similar by character comparison
        /// </summary>
        /// <param name="word">The word</param>
        /// <param name="tolerance">The error tolerance</param>
        /// <returns></returns>
        IDictionary<string, Fraction> FindWordsLike(string word, float tolerance = 0.75F);

        /// <summary>
        /// Returns the internal ID assigned to a word
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        int IdOf(string word);

        /// <summary>
        /// Return true if a word exists in the set
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        bool IsDefined(string word);

        /// <summary>
        /// Returns a random word
        /// </summary>
        string RandomWord();

        /// <summary>
        /// Appends a word to the set returning its assigned ID
        /// </summary>
        int Append(string word);

        /// <summary>
        /// Returns the count of all words
        /// </summary>
        int Count { get; }
    }
}