namespace LinqInfer.Learning
{
    public class ObjectVector<T>
    {
        public ObjectVector(T value, double[] attributes)
        {
            Attributes = attributes;
            Value = value;
        }

        public T Value { get; private set; }

        public double[] Attributes { get; private set; }
    }
}
