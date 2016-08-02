using System;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<ClassifiedObject<TInput, TClass>> ClassifyUsing<TInput, TClass>(this IEnumerable<TInput> data, Func<TInput, TClass> classf)
        {
            return data.Select(x => new ClassifiedObject<TInput, TClass>()
            {
                ObjectInstance = x,
                Classification = classf(x)
            });
        }

        public class ClassifiedObject<TInput, TClass>
        {
            public TInput ObjectInstance { get; internal set; }

            public TClass Classification { get; internal set; }
        }
    }
}
