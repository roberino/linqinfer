using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NetworkOutputSpecification
    {
        internal NetworkOutputSpecification(int outputModuleId, int outputVectorSize, ILossFunction lossFunction = null)
        {
            OutputModuleId = outputModuleId;
            OutputVectorSize = outputVectorSize;
            LossFunction = lossFunction ?? LossFunctions.Square;
        }

        public int OutputModuleId { get; }

        public int OutputVectorSize { get; }

        /// <summary>
        /// Returns a function for calculating errors
        /// </summary>
        public ILossFunction LossFunction { get; }

        /// <summary>
        /// Transforms the output
        /// </summary>
        public ISerialisableDataTransformation OutputTransformation { get; set; }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetName(nameof(NetworkOutputSpecification));
            doc.SetPropertyFromExpression(() => OutputVectorSize);
            doc.SetPropertyFromExpression(() => LossFunction, LossFunction.GetType().Name);

            if (OutputTransformation != null)
            {
                doc.WriteChildObject(OutputTransformation, new
                {
                    Property = nameof(OutputTransformation)
                });
            }

            return doc;
        }

        internal static NetworkOutputSpecification FromVectorDocument(PortableDataDocument doc,
            NetworkBuilderContext context)
        {
            NetworkOutputSpecification spec = null;

            var outputModuleId = doc.PropertyOrDefault(() => spec.OutputModuleId, 0);
            var outputVectorSize = doc.PropertyOrDefault(() => spec.OutputVectorSize, 0);
            var lossFuncStr = doc.PropertyOrDefault(() => spec.LossFunction, string.Empty);

            var lossFunc = context.LossFunctionFactory.Create(lossFuncStr);

            spec = new NetworkOutputSpecification(outputModuleId, outputVectorSize, lossFunc);

            if (doc.Children.Count > 0)
            {
                var query = doc.QueryChildren(new { Property = nameof(OutputTransformation) }).SingleOrDefault();

                if (query != null)
                {
                    spec.OutputTransformation = context
                        .TransformationFactory.Create(query);
                }
            }

            return spec;
        }
    }
}