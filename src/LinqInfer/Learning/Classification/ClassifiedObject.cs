namespace LinqInfer.Learning.Classification
{
    public static class ClassifiedObjectExtensions
    {
        public static ClassifiedObject<TInput, TClass> ClassifyAs<TInput, TClass>(this TInput instance, TClass classification)
        {
            return new ClassifiedObject<TInput, TClass>()
            {
                ObjectInstance = instance,
                Classification = classification
            };
        }

        public class ClassifiedObject<TInput, TClass>
        {
            public TInput ObjectInstance { get; internal set; }

            public TClass Classification { get; internal set; }
        }
    }
}
