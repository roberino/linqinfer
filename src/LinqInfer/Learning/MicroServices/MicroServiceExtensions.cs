using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Classification;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.MicroServices
{
    public static class MicroServiceExtensions
    {
        public static IHttpApiBuilder CreateClassifierService<TClass, TInput>(this IHttpApiBuilder apiBuilder, IObjectClassifier<TClass, TInput> classifier, string routePath = null)
        {
            if (TypeExtensions.IsAnonymous<TInput>()) throw new NotSupportedException("Anonymous types not supported - " + typeof(TInput).Name);

            apiBuilder
                .Bind(routePath ?? ("/classifiers/" + GetName<TInput>()), Verb.Post)
                .To<TInput, IEnumerable<ClassifyResult<TClass>>>(x => Task.FromResult(classifier.Classify(x)));

            return apiBuilder;
        }

        private static string GetName<T>()
        {
            var typeName = typeof(T).Name.ToLower();

            return new string(typeof(T).Name.ToLower().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        }
    }
}