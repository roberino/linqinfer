using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LinqInfer.Text.Tokenisers;

namespace LinqInfer.Text
{
    /// <summary>
    /// Various text conversion and processing utilities
    /// </summary>
    public static class TextUtility
    {
        /// <summary>
        /// Gets the encoding from the encoding enum
        /// </summary>
        public static Encoding GetEncoding(this TextEncoding encoding)
        {
            switch (encoding)
            {
                case TextEncoding.Ascii:
                    return Encoding.ASCII;
                case TextEncoding.Unicode:
                    return Encoding.Unicode;
                default:
                    return Encoding.UTF8;
            }
        }

        /// <summary>
        /// Gets encoded bytes for a string
        /// </summary>
        public static byte[] AsBytes(this string text, TextEncoding encoding = TextEncoding.UTF8)
        {
            if (text == null) return new byte[0];

            return encoding.GetEncoding().GetBytes(text);
        }

        /// <summary>
        /// Gets a string from encoded bytes
        /// </summary>
        public static string AsString(this byte[] data, TextEncoding encoding = TextEncoding.UTF8)
        {
            if (data == null) return null;

            return encoding.GetEncoding().GetString(data);
        }

        /// <summary>
        /// Returns a text reader from a block of text
        /// </summary>
        public static TextReader AsReader(this string text)
        {
            return new StringReader(text);
        }

        /// <summary>
        /// Opens a file as a tokenised document
        /// </summary>
        /// <param name="file">The file to open</param>
        /// <param name="id">The id to assign to the document</param>
        /// <param name="encoding">The text encoding (optional)</param>
        /// <param name="tokeniser">An optional tokeniser</param>
        /// <returns></returns>
        public static TokenisedTextDocument AsTokenisedDocument(FileInfo file, string id = null, Encoding encoding = null, ITokeniser tokeniser = null)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return new TokenisedTextDocument(id ?? Guid.NewGuid().ToString(), fs.Tokenise(encoding, tokeniser).ToList());
            }
        }

        /// <summary>
        /// Gets an enumeration of tokenised lines of text
        /// </summary>
        public static IEnumerable<IList<IToken>> AsTokenisedLines(this TextReader reader, ITokeniser tokeniser = null)
        {
            var tk = tokeniser ?? new Tokeniser();

            while (true)
            {
                var next = reader.ReadLine();

                if (next == null) yield break;

                yield return tk.Tokenise(next).ToList();
            }
        }
    }
}