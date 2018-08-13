using System;

namespace LinqInfer.Learning.Classification
{
    class XorNode : IEquatable<XorNode>
    {
        public bool X { get; set; }
        public bool Y { get; set; }
        public bool Output
        {
            get
            {
                return X ^ Y; 
            }
        }

        public bool Equals(XorNode other)
        {
            if (other == null) return false;

            return other.Output == Output;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as XorNode);
        }

        public override int GetHashCode()
        {
            return Output ? 1 : -1;
        }

        public override string ToString()
        {
            return string.Format("X:{0} Y:{1}", X, Y);
        }
    }
}