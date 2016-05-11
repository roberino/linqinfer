using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    public interface IFeature
    {
        string Key { get; }
        string Label { get; }
        int Index { get; }
        DistributionModel Model { get; }
        TypeCode DataType { get; }
    }

    internal class Feature : IFeature
    {
        public TypeCode DataType { get; set; }
        public int Index { get; set; }
        public string Key { get; set; }
        public string Label { get; set; }
        public DistributionModel Model { get; set; }

        public static IFeature[] CreateDefault(int vectorSize)
        {
            Contract.Assert(vectorSize > 0);

            return Enumerable
                    .Range(0, vectorSize)
                    .Select(n => new { label = n.ToString(), index = n })
                    .Select(m => new Feature() { Index = m.index, Label = m.label, Key = m.label, DataType = TypeCode.Object })
                    .ToArray();
        }

        public static IFeature[] CreateDefault(IEnumerable<string> labels, DistributionModel model = DistributionModel.Unknown)
        {
            Contract.Assert(labels != null && labels.Any());

            int i = 0;

            return labels.Select(l => new Feature()
            {
                Key = l.ToLower(),
                Index = i++,
                DataType = TypeCode.Object,
                Label = l,
                Model = model
            }).ToArray();
        }
    }
}