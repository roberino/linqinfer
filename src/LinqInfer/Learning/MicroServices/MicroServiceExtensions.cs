using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Classification;

namespace LinqInfer.Learning.MicroServices
{
    public static class MicroServiceExtensions
    {
        public static IHttpApiBuilder CreateClassifierService<TClass, TInput>(this IHttpApiBuilder apiBuilder, IObjectClassifier<TClass, TInput> classifier)
        {
            apiBuilder
                .Bind("/classifiers/" + GetName<TInput>(), Verb.Post);

            return apiBuilder;
        }

        private static string GetName<T>()
        {
            return typeof(T).Name.ToLower();
        }
    }
}