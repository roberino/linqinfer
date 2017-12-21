using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    public interface IFeature
    {
        /// <summary>
        /// A unique key for the feature
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The feature label
        /// </summary>
        string Label { get; }

        /// <summary>
        /// The feature vector index (base zero)
        /// </summary>
        int Index { get; }

        /// <summary>
        /// The vector model of the feature
        /// </summary>
        FeatureVectorModel Model { get; }

        /// <summary>
        /// The raw type code of the feature
        /// </summary>
        TypeCode DataType { get; }
    }

    internal class Feature : IFeature
    {
        public TypeCode DataType { get; set; }
        public int Index { get; set; }
        public string Key { get; set; }
        public string Label { get; set; }
        public FeatureVectorModel Model { get; set; }

        public override string ToString()
        {
            return Index + ':' + Label ?? Key;
        }

        public static IFeature[] CreateDefaults(int vectorSize, TypeCode dataType = TypeCode.Object, string labelTemplate = "{0}")
        {
            Contract.Assert(vectorSize > 0);

            return Enumerable
                    .Range(0, vectorSize)
                    .Select(n => new { label = string.Format(labelTemplate, n + 1), index = n })
                    .Select(m => new Feature() { Index = m.index, Label = m.label, Key = ToKey(m.label), DataType = dataType })
                    .ToArray();
        }

        public static IFeature[] CreateDefaults(IEnumerable<string> labels, FeatureVectorModel model = FeatureVectorModel.Magnitudinal)
        {
            Contract.Assert(labels != null && labels.Any());

            int i = 0;

            return labels.Select(l => new Feature()
            {
                Key = ToKey(l),
                Index = i++,
                DataType = TypeCode.Object,
                Label = l,
                Model = model
            }).ToArray();
        }

        private static string ToKey(string label)
        {
            return new string(label.Replace(' ', '_').Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray()).ToLower();
        }
    }
}