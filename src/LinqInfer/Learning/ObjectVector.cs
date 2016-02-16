namespace LinqInfer.Learning
{
    public class ObjectVector<T>
    {
        public ObjectVector(T value, float[] attributes)
        {
            Attributes = attributes;
            Value = value;
        }

        public T Value { get; private set; }

        public float[] Attributes { get; private set; }
    }
}
