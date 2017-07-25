using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text
{
    public sealed class DocumentTermWeightingData : IEquatable<DocumentTermWeightingData>
    {
        internal DocumentTermWeightingData()
        {
        }

        public static double DefaultCalculationMethod(IList<DocumentTermWeightingData> weights)
        {
            var score = weights.Aggregate(1d, (t, x) =>
            {
                var idf = Math.Log(x.DocumentCount / (double)x.DocumentFrequency);

                return t * (idf + 1) * x.TermFrequency;
            });

            return score;
        }

        public static double DefaultCalculationMethodNoAdjust(IList<DocumentTermWeightingData> weights)
        {
            var score = weights.Aggregate(1d, (t, x) =>
            {
                var idf = Math.Log(x.DocumentCount / (double)x.DocumentFrequency);

                return t * idf * x.TermFrequency;
            });

            return score;
        }

        public static double NormalisedCalculationMethod(IList<DocumentTermWeightingData> weights)
        {
            return weights.Aggregate(0d, (t, x) =>
            {
                var idf = Math.Log(x.DocumentCount / (double)x.DocumentFrequency);

                var vect = ColumnVector1D.Create(idf, x.TermFrequency).Normalise().GetUnderlyingArray();
                return t + vect[0] * vect[1];
            });
        }

        public bool Equals(DocumentTermWeightingData other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            return string.Equals(other.Term, Term);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DocumentTermWeightingData);
        }

        public override int GetHashCode()
        {
            return Term.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}/{2})", Term, TermFrequency, DocumentFrequency);
        }

        /// <summary>
        /// The term text
        /// </summary>
        public string Term { get; internal set; }

        /// <summary>
        /// The total documents in the index
        /// </summary>
        public long DocumentCount { get; internal set; }

        /// <summary>
        /// The total terms in the document
        /// </summary>
        public long? TermCount { get; internal set; }

        /// <summary>
        /// The frequency the term occurs in the document
        /// </summary>
        public long TermFrequency { get; internal set; }

        /// <summary>
        /// The number of documents in which the term occurs
        /// </summary>
        public long DocumentFrequency { get; internal set; }
    }
}