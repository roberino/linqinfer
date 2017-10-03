using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LinqInfer.Microservices.Text
{
    public class FilterParser
    {
        public IEnumerable<TransformationOperation> Parse(string filterText)
        {
            if (string.IsNullOrEmpty(filterText)) yield break;

            var regex = new Regex(@"(\w+)\(((\d+,?)*)\)");

            foreach(var part in filterText.Split('|'))
            {
                var bits = regex.Match(part);

                yield return new TransformationOperation()
                {
                    OperationName = bits.Groups[1].Value,
                    Parameters = bits.Groups[2].Value.Split(',')
                };
            }
        }
    }

    public class TransformationOperation
    {
        public string OperationName { get; set; }

        public string[] Parameters { get; set; }
    }
}