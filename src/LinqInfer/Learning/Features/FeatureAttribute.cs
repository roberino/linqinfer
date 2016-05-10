using LinqInfer.Maths.Probability;
using System;

namespace LinqInfer.Learning.Features
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class FeatureAttribute : Attribute
    {
        // public int Order { get; set; } ??
        public bool Ignore { get; set; }
        public Type Converter { get; set; }
        public string SetName { get; set; }
        public DistributionModel Model { get; set; }
    }
}
