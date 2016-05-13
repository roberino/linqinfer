using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using System;

namespace LinqInfer.Text
{
    public static class TextFuctions
    {
        /// <summary>
        ///     Compute the difference ratio (as a fraction) between two strings based on Levenshtein distance.
        /// </summary>
        public static Fraction ComputeLevenshteinDifference(this string s, string t)
        {
            if (s == null || t == null) return Fraction.Zero;

            var max = Math.Max(s.Length, t.Length);
            var ld = ComputeLevenshteinDistance(s, t);

            return (max - ld).OutOf(max);
        }

        /// <summary>
        ///     Compute the Levenshtein distance between two strings.
        /// </summary>
        /// <remarks>
        ///     http://www.dotnetperls.com/levenshtein
        /// </remarks>
        public static int ComputeLevenshteinDistance(this string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (var i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (var j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (var i = 1; i <= n; i++)
            {
                //Step 4
                for (var j = 1; j <= m; j++)
                {
                    // Step 5
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}
