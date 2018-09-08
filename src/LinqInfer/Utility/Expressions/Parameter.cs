using System;

namespace LinqInfer.Utility.Expressions
{
    public sealed class Parameter
    {
        internal Parameter(string name, Type type, int index)
        {
            Name = name;
            Type = type;
            Index = index;
        }

        public string Name { get; }
        public Type Type { get; }
        public int Index { get; }
    }
}