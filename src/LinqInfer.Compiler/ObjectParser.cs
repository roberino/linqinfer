using LinqInfer.Utility.Expressions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqInfer.Compiler
{
    class ObjectParser : ISourceCodeParser
    {
        public bool CanParse(SourceCode sourceCode)
        {
            return sourceCode.MimeType == KnownMimeTypes.Json;
        }

        public LambdaExpression Parse(SourceCode sourceCode, Func<Parameter, Type> parameterBinder, Type outputType = null)
        {
            //var reader = new JsonTextReader(new StringReader(sourceCode.Code));

            //var jsonObj = (dynamic)JObject.Load(reader);

            var jsonObj = JsonConvert.DeserializeObject<IDictionary<string, object>>(sourceCode.Code);

            return Expression.Lambda(Expression.Constant(jsonObj));
        }
    }
}