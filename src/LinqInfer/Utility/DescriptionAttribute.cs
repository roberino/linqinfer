using System;

namespace LinqInfer.Utility
{
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public string Description { get; }
    }
}